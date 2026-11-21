// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Domain.Enums;
using AgentCoreSystem.Module.Organization.Application.Members.Queries;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Chat.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh phê duyệt và thực thi yêu cầu gọi công cụ đang chờ duyệt trong kênh trò chuyện.
    /// </summary>
    /// <param name="PendingToolCallId">Mã định danh yêu cầu gọi công cụ (UUIDv7).</param>
    public record ConfirmToolCallCommand(Guid PendingToolCallId) : ICommand<bool>;

    /// <summary>
    /// Handler xử lý phê duyệt lệnh gọi công cụ và tiếp tục chu trình hội thoại với LLM.
    /// </summary>
    public class ConfirmToolCallCommandHandler(
        IChatDbContext dbContext,
        ISender sender,
        IRestApiExecutor restApiExecutor,
        IMessageDispatcherService messageDispatcher,
        ICurrentUser? currentUser = null)
        : ICommandHandler<ConfirmToolCallCommand, bool>
    {
        /// <inheritdoc />
        public async Task<Result<bool>> Handle(
            ConfirmToolCallCommand request,
            CancellationToken cancellationToken)
        {
            var pendingCall = await dbContext.PendingToolCalls
                .FirstOrDefaultAsync(p => p.Id == request.PendingToolCallId, cancellationToken);

            if (pendingCall == null)
            {
                return Result.Failure<bool>(Error.NotFound(
                    "PendingToolCall.NotFound",
                    $"Không tìm thấy yêu cầu gọi công cụ với mã '{request.PendingToolCallId}'."));
            }

            if (pendingCall.Status != PendingToolCallStatus.Pending)
            {
                return Result.Failure<bool>(Error.Conflict(
                    "PendingToolCall.NotPending",
                    $"Yêu cầu gọi công cụ '{request.PendingToolCallId}' không ở trạng thái chờ duyệt ({pendingCall.Status})."));
            }

            if (pendingCall.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                pendingCall.Expire();
                await dbContext.SaveChangesAsync(cancellationToken);

                return Result.Failure<bool>(Error.Validation(
                    "PendingToolCall.Expired",
                    $"Yêu cầu gọi công cụ '{request.PendingToolCallId}' đã hết hạn chờ phê duyệt."));
            }

            // 1. Kiểm tra quyền của người dùng hiện tại (phải là thành viên người dùng thực MemberType.Human trong kênh)
            if (currentUser?.UserId is { } currentUserId && currentUserId != Guid.Empty)
            {
                var memberResult = await sender.Send(new GetMemberByUserIdAndChannelQuery(currentUserId, pendingCall.ChannelId), cancellationToken);
                if (memberResult.IsFailure)
                {
                    return Result.Failure<bool>(Error.Forbidden(
                        "ToolCall.Forbidden",
                        "Người dùng không thuộc kênh trò chuyện này."));
                }

                if (memberResult.Value.Type != MemberType.Human)
                {
                    return Result.Failure<bool>(Error.Forbidden(
                        "ToolCall.OnlyHumanAllowed",
                        "Chỉ thành viên người dùng thực mới có quyền phê duyệt công cụ."));
                }
            }

            // 2. Tìm thông tin định nghĩa công cụ để thực thi
            var toolsResult = await sender.Send(new GetToolDefinitionsQuery(MaskSecrets: false), cancellationToken);
            var toolDef = toolsResult.Value?.FirstOrDefault(t => t.Name.Equals(pendingCall.ToolName, StringComparison.OrdinalIgnoreCase));

            if (toolDef == null)
            {
                return Result.Failure<bool>(Error.NotFound(
                    "ToolDefinition.NotFound",
                    $"Không tìm thấy định nghĩa công cụ '{pendingCall.ToolName}'."));
            }

            // 3. Phân giải Headers và Arguments
            var headers = SecretMaskingHelper.ParseHeaders(toolDef.HeadersConfig);
            IDictionary<string, object?>? arguments = null;
            if (!string.IsNullOrWhiteSpace(pendingCall.ArgumentsJson))
            {
                try
                {
                    arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(pendingCall.ArgumentsJson);
                }
                catch (JsonException)
                {
                    arguments = new Dictionary<string, object?>();
                }
            }

            // 4. Thực thi công cụ qua IRestApiExecutor
            var execRequest = new RestApiExecutionRequest
            {
                Url = toolDef.EndpointUrl,
                Method = toolDef.HttpMethod,
                Headers = headers,
                Arguments = arguments,
                TimeoutSeconds = toolDef.TimeoutSeconds
            };

            var execResult = await restApiExecutor.ExecuteAsync(execRequest, cancellationToken);
            var toolOutput = execResult.IsSuccess
                ? execResult.ResponsePayload
                : (execResult.ErrorMessage ?? "Thực thi công cụ thất bại.");

            // 5. Đánh dấu đã phê duyệt
            pendingCall.Confirm();

            // 6. Lưu tin nhắn kết quả của tool vào DB
            var originalAssistantMsg = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == pendingCall.ChatMessageId, cancellationToken);

            var toolMessage = new ChatMessage(
                channelId: pendingCall.ChannelId,
                senderMemberId: originalAssistantMsg?.SenderMemberId ?? Guid.Empty,
                senderDisplayName: $"{originalAssistantMsg?.SenderDisplayName ?? "Agent"} (Tool: {pendingCall.ToolName})",
                isAgent: true,
                content: toolOutput,
                callDepth: (originalAssistantMsg?.CallDepth ?? 0) + 1,
                parentMessageId: pendingCall.ChatMessageId);

            dbContext.Messages.Add(toolMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 7. Ghi nhật ký thực thi công cụ (ToolExecutionLog)
            try
            {
                var channelMembers = await sender.Send(new GetChannelMembersQuery(pendingCall.ChannelId), cancellationToken);
                var agentMember = channelMembers.Value?.FirstOrDefault(m => m.Id == originalAssistantMsg?.SenderMemberId);
                var agentProfileId = agentMember?.AgentProfileId ?? Guid.Empty;

                await sender.Send(new RecordToolExecutionCommand(
                    ToolDefinitionId: toolDef.Id,
                    AgentProfileId: agentProfileId,
                    CallerType: ToolCallerType.Chat,
                    ExecutionSourceId: pendingCall.ChatMessageId,
                    InputArguments: pendingCall.ArgumentsJson,
                    ResponsePayload: execResult.ResponsePayload,
                    StatusCode: execResult.StatusCode,
                    DurationMs: execResult.DurationMs,
                    IsSuccess: execResult.IsSuccess,
                    ErrorMessage: execResult.ErrorMessage), cancellationToken);
            }
            catch
            {
                // Không gián đoạn luồng chính nếu ghi log gặp lỗi
            }

            // 8. Tiếp tục chu trình hội thoại với LLM
            await messageDispatcher.ResumeAfterToolCallAsync(pendingCall.Id, cancellationToken);

            return Result.Success(true);
        }
    }
}
