// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Domain.Enums;
using AgentCoreSystem.Module.Organization.Application.Members.Queries;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Application.Contracts;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Chat.Application.Tools.Commands
{
    /// <summary>
    /// Lệnh từ chối yêu cầu thực thi công cụ đang chờ duyệt trong kênh trò chuyện.
    /// </summary>
    /// <param name="PendingToolCallId">Mã định danh yêu cầu gọi công cụ (UUIDv7).</param>
    /// <param name="Reason">Lý do từ chối (tùy chọn) để gửi cho AI Agent.</param>
    public record RejectToolCallCommand(Guid PendingToolCallId, string? Reason = null) : ICommand<bool>;

    /// <summary>
    /// Handler xử lý từ chối lệnh gọi công cụ và kích hoạt Agent phản hồi điều chỉnh.
    /// </summary>
    public class RejectToolCallCommandHandler(
        IChatDbContext dbContext,
        ISender sender,
        IMessageDispatcherService messageDispatcher,
        ICurrentUser? currentUser = null)
        : ICommandHandler<RejectToolCallCommand, bool>
    {
        /// <inheritdoc />
        public async Task<Result<bool>> Handle(
            RejectToolCallCommand request,
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
                        "Chỉ thành viên người dùng thực mới có quyền từ chối công cụ."));
                }
            }

            // 2. Đánh dấu đã từ chối
            pendingCall.Reject();

            // 3. Lưu tin nhắn thông báo từ chối vào DB
            var rejectionContent = string.IsNullOrWhiteSpace(request.Reason)
                ? "User rejected tool execution."
                : $"User rejected tool execution: {request.Reason}";

            var originalAssistantMsg = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == pendingCall.ChatMessageId, cancellationToken);

            var toolMessage = new ChatMessage(
                channelId: pendingCall.ChannelId,
                senderMemberId: originalAssistantMsg?.SenderMemberId ?? Guid.Empty,
                senderDisplayName: $"{originalAssistantMsg?.SenderDisplayName ?? "Agent"} (Tool: {pendingCall.ToolName})",
                isAgent: true,
                content: rejectionContent,
                callDepth: (originalAssistantMsg?.CallDepth ?? 0) + 1,
                parentMessageId: pendingCall.ChatMessageId);

            dbContext.Messages.Add(toolMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 4. Tiếp tục chu trình hội thoại để Agent phản hồi lịch sự
            await messageDispatcher.ResumeAfterToolCallAsync(pendingCall.Id, cancellationToken);

            return Result.Success(true);
        }
    }
}
