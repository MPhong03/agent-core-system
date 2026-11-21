// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Agents.Application.Common;
using AgentCoreSystem.Module.Agents.Application.Tools.Commands;
using AgentCoreSystem.Module.Agents.Application.Tools.DTOs;
using AgentCoreSystem.Module.Agents.Application.Tools.Queries;
using AgentCoreSystem.Module.Agents.Domain.Enums;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.Commands;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Application.Tools.DTOs;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Module.Chat.Domain.Enums;
using AgentCoreSystem.Module.Chat.Infrastructure.Hubs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Infrastructure.Http;
using AgentCoreSystem.Shared.Infrastructure.LLM;
using AgentCoreSystem.Shared.Kernel.Primitives;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Module.Chat.Infrastructure.Services
{
    /// <summary>
    /// Triển khai dịch vụ điều phối tin nhắn @mention và kích hoạt hội thoại streaming LLM cho các Agent.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Xếp hàng tuần tự xử lý Agent, kiểm soát độ sâu CallDepth (ngắt khi &gt;= 3), hỗ trợ Tool Calling vòng lặp (với tối đa 5 bước lặp) và cơ chế phê duyệt tương tác (Human-in-the-loop).</para>
    /// <para><strong>When:</strong> Được gọi từ SignalRChatHub hoặc API khi tin nhắn có gắn thẻ nhân sự AI hoặc sau khi người dùng xác nhận/từ chối lệnh gọi công cụ.</para>
    /// <para><strong>Why:</strong> Đảm bảo luồng hội thoại đa tác nhân mượt mà, ngăn ngừa đệ quy vô hạn và đồng bộ dữ liệu vào ChatDbContext.</para>
    /// </remarks>
    public class MessageDispatcherService(
        IChatDbContext dbContext,
        ISender sender,
        ILLMProviderFactory llmProviderFactory,
        IHubContext<SignalRChatHub> hubContext,
        IRestApiExecutor? restApiExecutor = null,
        ILogger<MessageDispatcherService>? logger = null) : IMessageDispatcherService
    {
        // ── Constants & Configuration ──

        private const int MaxCallDepth = 3;
        private const int SlidingWindowSize = 20;
        private const int MaxToolIterations = 5;

        // ── Dispatcher Entry Point ──

        /// <inheritdoc />
        public async Task DispatchMessageAsync(
            ChatMessageDto userMessage,
            List<Guid> mentionedAgentIds,
            CancellationToken ct = default)
        {
            // 1. Kiểm tra vòng lặp vô hạn (Loop Protection)
            if (userMessage.CallDepth >= MaxCallDepth)
            {
                logger?.LogWarning(
                    "Dừng điều phối tin nhắn vì độ sâu cuộc gọi đạt giới hạn: CallDepth={CallDepth}, MessageId={MessageId}",
                    userMessage.CallDepth,
                    userMessage.Id);
                return;
            }

            if (mentionedAgentIds is null || mentionedAgentIds.Count == 0)
            {
                return;
            }

            var distinctAgentIds = mentionedAgentIds.Distinct().ToList();

            // 2. Lấy thông tin thành viên trong kênh
            var channelMembersResult = await sender.Send(new GetChannelMembersQuery(userMessage.ChannelId), ct);
            if (channelMembersResult.IsFailure)
            {
                logger?.LogWarning(
                    "Không thể truy vấn danh sách thành viên cho kênh {ChannelId}: {Error}",
                    userMessage.ChannelId,
                    channelMembersResult.Error.Description);
                return;
            }

            // 3. Lấy thông tin hồ sơ Agent trong hệ thống
            var agentProfilesResult = await sender.Send(new GetAgentProfilesQuery(), ct);
            if (agentProfilesResult.IsFailure)
            {
                logger?.LogWarning(
                    "Không thể truy vấn danh mục hồ sơ Agent: {Error}",
                    agentProfilesResult.Error.Description);
                return;
            }

            var profilesMap = agentProfilesResult.Value.ToDictionary(p => p.Id);

            // 4. Xử lý tuần tự từng Agent trong danh sách nhắc tên (Sequential Queue)
            foreach (var agentId in distinctAgentIds)
            {
                ct.ThrowIfCancellationRequested();

                var member = channelMembersResult.Value.FirstOrDefault(m =>
                    m.Id == agentId || m.AgentProfileId == agentId);

                if (member is null || member.Type != MemberType.Agent || !member.AgentProfileId.HasValue)
                {
                    continue;
                }

                if (!profilesMap.TryGetValue(member.AgentProfileId.Value, out var profile))
                {
                    logger?.LogWarning(
                        "Không tìm thấy cấu hình AgentProfile {ProfileId} cho thành viên {MemberId}",
                        member.AgentProfileId.Value,
                        member.Id);
                    continue;
                }

                try
                {
                    await ProcessAgentResponseAsync(userMessage, member, profile, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger?.LogError(
                        ex,
                        "Lỗi khi xử lý phản hồi Agent cho thành viên {MemberId} trong kênh {ChannelId}",
                        member.Id,
                        userMessage.ChannelId);
                }
            }
        }

        // ── Human-in-the-Loop Resumption ──

        /// <inheritdoc />
        public async Task ResumeAfterToolCallAsync(Guid pendingToolCallId, CancellationToken ct = default)
        {
            var pendingCall = await dbContext.PendingToolCalls
                .FirstOrDefaultAsync(p => p.Id == pendingToolCallId, ct);

            if (pendingCall == null)
            {
                logger?.LogWarning("Không tìm thấy yêu cầu gọi công cụ với mã {PendingToolCallId}", pendingToolCallId);
                return;
            }

            var channelGroup = $"channel_{pendingCall.ChannelId}";
            var originalAssistantMsg = await dbContext.Messages
                .FirstOrDefaultAsync(m => m.Id == pendingCall.ChatMessageId, ct);

            var channelMembersResult = await sender.Send(new GetChannelMembersQuery(pendingCall.ChannelId), ct);
            if (channelMembersResult.IsFailure)
            {
                logger?.LogWarning("Không thể lấy danh sách thành viên kênh {ChannelId}", pendingCall.ChannelId);
                await NotifyClientResumeFailureAsync(channelGroup, Guid.Empty, "Agent", ct);
                return;
            }

            var member = channelMembersResult.Value.FirstOrDefault(m =>
                m.Id == originalAssistantMsg?.SenderMemberId ||
                (originalAssistantMsg != null && m.DisplayName == originalAssistantMsg.SenderDisplayName));

            if (member == null || !member.AgentProfileId.HasValue)
            {
                logger?.LogWarning("Không xác định được thành viên Agent tương ứng cho tin nhắn {MessageId}", pendingCall.ChatMessageId);
                await NotifyClientResumeFailureAsync(channelGroup, Guid.Empty, "Agent", ct);
                return;
            }

            var profilesResult = await sender.Send(new GetAgentProfilesQuery(), ct);
            if (profilesResult.IsFailure)
            {
                await NotifyClientResumeFailureAsync(channelGroup, member.Id, member.DisplayName, ct);
                return;
            }

            var profile = profilesResult.Value.FirstOrDefault(p => p.Id == member.AgentProfileId.Value);
            if (profile == null)
            {
                await NotifyClientResumeFailureAsync(channelGroup, member.Id, member.DisplayName, ct);
                return;
            }

            var toolsResult = await sender.Send(new GetAgentToolsQuery(profile.Id, MaskSecrets: false), ct);
            var activeTools = toolsResult.IsSuccess ? toolsResult.Value : new List<ToolDefinitionDto>();
            var toolDefs = activeTools.Select(t => new LlmToolDefinitionPayload(
                t.Name,
                t.Description,
                t.ParametersSchema,
                "function"
            )).ToList();

            var recentMessages = await dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ChannelId == pendingCall.ChannelId)
                .OrderByDescending(m => m.CreatedDate)
                .ThenByDescending(m => m.Id)
                .Take(SlidingWindowSize)
                .ToListAsync(ct);

            var orderedMessages = recentMessages
                .OrderBy(m => m.CreatedDate)
                .ThenBy(m => m.Id)
                .ToList();

            var conversationPayload = new List<ChatMessagePayload>();
            if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
            {
                conversationPayload.Add(new ChatMessagePayload("system", profile.SystemPrompt));
            }

            foreach (var m in orderedMessages)
            {
                if (m.Id == pendingCall.ChatMessageId)
                {
                    conversationPayload.Add(new ChatMessagePayload(
                        Role: "assistant",
                        Content: m.Content,
                        ToolCalls: new List<LlmToolCallPayload>
                        {
                            new(pendingCall.ToolCallId, pendingCall.ToolName, pendingCall.ArgumentsJson)
                        }));
                }
                else if (m.ParentMessageId == pendingCall.ChatMessageId && m.Id != pendingCall.ChatMessageId)
                {
                    conversationPayload.Add(new ChatMessagePayload(
                        Role: "tool",
                        Content: m.Content,
                        ToolCallId: pendingCall.ToolCallId));
                }
                else
                {
                    conversationPayload.Add(new ChatMessagePayload(
                        Role: m.IsAgent ? "assistant" : "user",
                        Content: m.Content));
                }
            }

            var providerName = profile.Provider.ToString();
            var llmClient = llmProviderFactory.GetClient(providerName);

            try
            {
                var response = await llmClient.ChatWithToolsAsync(
                    profile.ModelName,
                    conversationPayload,
                    toolDefs,
                    profile.Temperature,
                    ct);

                var finalContent = response.Content ?? string.Empty;
                var finalMsgId = Guid.CreateVersion7();

                var finalMessage = new ChatMessage(
                    pendingCall.ChannelId,
                    member.Id,
                    member.DisplayName,
                    isAgent: true,
                    content: finalContent,
                    callDepth: (originalAssistantMsg?.CallDepth ?? 0) + 1,
                    parentMessageId: pendingCall.ChatMessageId,
                    id: finalMsgId);

                dbContext.Messages.Add(finalMessage);
                await dbContext.SaveChangesAsync(ct);

                await hubContext.Clients.Group(channelGroup).SendAsync("ReceiveMessageChunk", finalMsgId, member.Id, finalContent, ct);
                await hubContext.Clients.Group(channelGroup).SendAsync("ReceiveMessageCompleted", finalMsgId, member.Id, member.DisplayName, finalContent, ct);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Lỗi khi tiếp tục hội thoại LLM sau phê duyệt công cụ cho kênh {ChannelId}", pendingCall.ChannelId);
                await NotifyClientResumeFailureAsync(channelGroup, member.Id, member.DisplayName, ct);
            }
        }

        private async Task NotifyClientResumeFailureAsync(
            string channelGroup,
            Guid memberId,
            string memberDisplayName,
            CancellationToken ct)
        {
            const string errorMessage = "[Hệ thống: Không thể hoàn tất câu trả lời sau khi xử lý công cụ.]";
            var errorMsgId = Guid.CreateVersion7();
            try
            {
                await hubContext.Clients
                    .Group(channelGroup)
                    .SendAsync("ReceiveMessageCompleted", errorMsgId, memberId, memberDisplayName, errorMessage, CancellationToken.None);
            }
            catch (Exception sendEx)
            {
                logger?.LogWarning(sendEx, "Không thể gửi ReceiveMessageCompleted khi xử lý lỗi ResumeAfterToolCallAsync");
            }
        }

        // ── LLM Invocation & Streaming ──

        /// <summary>
        /// Xử lý lấy context, gọi streaming qua LLM provider và lưu kết quả phản hồi của một Agent.
        /// </summary>
        /// <param name="userMessage">Tin nhắn của người dùng hoặc tin nhắn kích hoạt.</param>
        /// <param name="member">Thông tin thành viên kênh của Agent.</param>
        /// <param name="profile">Hồ sơ cấu hình LLM của Agent.</param>
        /// <param name="ct">Token hủy tác vụ.</param>
        private async Task ProcessAgentResponseAsync(
            ChatMessageDto userMessage,
            MemberDto member,
            AgentProfileDto profile,
            CancellationToken ct)
        {
            var channelGroup = $"channel_{userMessage.ChannelId}";
            var agentMessageId = Guid.CreateVersion7();

            try
            {
                // 1. Truy vấn 20 tin nhắn gần nhất làm Sliding Window Context
                var recentMessages = await dbContext.Messages
                    .AsNoTracking()
                    .Where(m => m.ChannelId == userMessage.ChannelId)
                    .OrderByDescending(m => m.CreatedDate)
                    .ThenByDescending(m => m.Id)
                    .Take(SlidingWindowSize)
                    .ToListAsync(ct);

                var orderedHistory = recentMessages
                    .OrderBy(m => m.CreatedDate)
                    .ThenBy(m => m.Id)
                    .Select(m => new ChatMessagePayload(
                        Role: m.IsAgent ? "assistant" : "user",
                        Content: m.Content))
                    .ToList();

                // 2. Ghép SystemPrompt từ AgentProfile tương ứng
                var payload = new List<ChatMessagePayload>();
                if (!string.IsNullOrWhiteSpace(profile.SystemPrompt))
                {
                    payload.Add(new ChatMessagePayload("system", profile.SystemPrompt));
                }
                payload.AddRange(orderedHistory);

                // 3. Khởi tạo LLM Client theo cấu hình Provider
                var providerName = profile.Provider.ToString();
                var llmClient = llmProviderFactory.GetClient(providerName);

                // 4. Lấy danh sách công cụ được phân quyền cho Agent (Unmasked Secrets)
                var activeTools = new List<ToolDefinitionDto>();
                try
                {
                    var toolsResult = await sender.Send(new GetAgentToolsQuery(profile.Id, MaskSecrets: false), ct);
                    if (toolsResult is { IsSuccess: true, Value: not null })
                    {
                        activeTools = toolsResult.Value;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Không thể truy vấn danh sách công cụ của Agent {ProfileId}", profile.Id);
                }

                // Nếu Agent không có công cụ nào -> Giữ nguyên luồng stream token văn bản thuần túy
                if (activeTools.Count == 0)
                {
                    var fullContentBuilder = new StringBuilder();

                    await foreach (var chunk in llmClient.StreamChatAsync(profile.ModelName, payload, profile.Temperature, ct))
                    {
                        fullContentBuilder.Append(chunk);

                        await hubContext.Clients
                            .Group(channelGroup)
                            .SendAsync("ReceiveMessageChunk", agentMessageId, member.Id, chunk, ct);
                    }

                    var fullContent = fullContentBuilder.ToString();

                    var saveCommand = new SaveChatMessageCommand(
                        ChannelId: userMessage.ChannelId,
                        SenderMemberId: member.Id,
                        SenderDisplayName: member.DisplayName,
                        IsAgent: true,
                        Content: fullContent,
                        CallDepth: userMessage.CallDepth + 1,
                        ParentMessageId: userMessage.Id,
                        MentionIds: null,
                        MessageId: agentMessageId);

                    var saveResult = await sender.Send(saveCommand, ct);
                    if (saveResult.IsFailure)
                    {
                        logger?.LogError(
                            "Không thể lưu tin nhắn phản hồi của Agent vào DB: {Error}",
                            saveResult.Error.Description);
                    }

                    await hubContext.Clients
                        .Group(channelGroup)
                        .SendAsync("ReceiveMessageCompleted", agentMessageId, member.Id, member.DisplayName, fullContent, ct);

                    return;
                }

                // 5. Chu trình gọi Tool (Tool Calling Loop với MaxToolIterations = 5)
                var toolDefs = activeTools.Select(t => new LlmToolDefinitionPayload(
                    t.Name,
                    t.Description,
                    t.ParametersSchema,
                    "function"
                )).ToList();

                int iterations = 0;
                var conversationPayload = new List<ChatMessagePayload>(payload);
                bool completed = false;

                while (iterations < MaxToolIterations)
                {
                    iterations++;

                    var response = await llmClient.ChatWithToolsAsync(
                        profile.ModelName,
                        conversationPayload,
                        toolDefs,
                        profile.Temperature,
                        ct);

                    if (response.HasToolCalls && response.ToolCalls != null && response.ToolCalls.Count > 0)
                    {
                        // Kiểm tra xem có cần sự phê duyệt của người dùng hay không
                        bool needsApproval = false;
                        if (profile.ToolExecutionMode == ToolExecutionMode.NeedApprove)
                        {
                            needsApproval = true;
                        }
                        else if (profile.ToolExecutionMode == ToolExecutionMode.SemiAuto)
                        {
                            foreach (var call in response.ToolCalls)
                            {
                                var matchedTool = activeTools.FirstOrDefault(t =>
                                    t.Name.Equals(call.Name, StringComparison.OrdinalIgnoreCase));
                                if (matchedTool != null && matchedTool.RequiresConfirmation)
                                {
                                    needsApproval = true;
                                    break;
                                }
                            }
                        }

                        if (needsApproval)
                        {
                            // Lưu tin nhắn trợ lý yêu cầu gọi công cụ vào DB
                            var assistantMsgId = Guid.CreateVersion7();
                            var assistantContent = !string.IsNullOrWhiteSpace(response.Content)
                                ? response.Content
                                : $"[Yêu cầu gọi công cụ: {string.Join(", ", response.ToolCalls.Select(c => c.Name))}]";

                            var assistantMessage = new ChatMessage(
                                userMessage.ChannelId,
                                member.Id,
                                member.DisplayName,
                                isAgent: true,
                                content: assistantContent,
                                callDepth: userMessage.CallDepth + 1,
                                parentMessageId: userMessage.Id,
                                id: assistantMsgId);

                            dbContext.Messages.Add(assistantMessage);

                            // Lưu bản ghi PendingToolCall
                            PendingToolCall? primaryPendingCall = null;
                            foreach (var call in response.ToolCalls)
                            {
                                var pendingCall = new PendingToolCall(
                                    userMessage.ChannelId,
                                    assistantMsgId,
                                    call.Id,
                                    call.Name,
                                    call.Arguments,
                                    DateTimeOffset.UtcNow.AddMinutes(5));

                                dbContext.PendingToolCalls.Add(pendingCall);
                                primaryPendingCall ??= pendingCall;
                            }

                            await dbContext.SaveChangesAsync(ct);

                            // Phát sóng sự kiện ToolApprovalRequested ra SignalR channelGroup chuẩn mực
                            if (primaryPendingCall != null)
                            {
                                var approvalPayload = new ToolApprovalRequestPayload(
                                    primaryPendingCall.Id,
                                    userMessage.ChannelId,
                                    assistantMsgId,
                                    primaryPendingCall.ToolCallId,
                                    primaryPendingCall.ToolName,
                                    primaryPendingCall.ArgumentsJson,
                                    primaryPendingCall.ExpiresAt);

                                await hubContext.Clients.Group(channelGroup)
                                    .SendAsync("ToolApprovalRequested", approvalPayload, ct);
                            }

                            // Dừng lượt (awaiting human confirmation/rejection)
                            return;
                        }
                        else
                        {
                            // Chế độ Auto hoặc SemiAuto không yêu cầu phê duyệt -> Thực thi ngay lập tức
                            conversationPayload.Add(new ChatMessagePayload(
                                Role: "assistant",
                                Content: response.Content,
                                ToolCalls: response.ToolCalls));

                            var assistantMsgId = Guid.CreateVersion7();
                            var assistantContent = !string.IsNullOrWhiteSpace(response.Content)
                                ? response.Content
                                : $"[Thực thi công cụ: {string.Join(", ", response.ToolCalls.Select(c => c.Name))}]";

                            var assistantMsg = new ChatMessage(
                                userMessage.ChannelId,
                                member.Id,
                                member.DisplayName,
                                isAgent: true,
                                content: assistantContent,
                                callDepth: userMessage.CallDepth + 1,
                                parentMessageId: userMessage.Id,
                                id: assistantMsgId);

                            dbContext.Messages.Add(assistantMsg);

                            foreach (var call in response.ToolCalls)
                            {
                                // Phát sóng ToolExecuting ra SignalR channelGroup chuẩn mực
                                await hubContext.Clients.Group(channelGroup)
                                    .SendAsync("ToolExecuting", call.Id, call.Name, call.Arguments, ct);

                                var execResult = await ExecuteToolCallAsync(activeTools, call, ct);
                                var toolOutput = execResult.IsSuccess
                                    ? execResult.ResponsePayload
                                    : (execResult.ErrorMessage ?? "Tool execution failed.");

                                var toolMsg = new ChatMessage(
                                    userMessage.ChannelId,
                                    member.Id,
                                    $"{member.DisplayName} (Tool: {call.Name})",
                                    isAgent: true,
                                    content: toolOutput,
                                    callDepth: userMessage.CallDepth + 1,
                                    parentMessageId: assistantMsgId);

                                dbContext.Messages.Add(toolMsg);

                                // Ghi nhật ký ToolExecutionLog
                                try
                                {
                                    var matchedTool = activeTools.FirstOrDefault(t => t.Name.Equals(call.Name, StringComparison.OrdinalIgnoreCase));
                                    if (matchedTool != null)
                                    {
                                        await sender.Send(new RecordToolExecutionCommand(
                                            ToolDefinitionId: matchedTool.Id,
                                            AgentProfileId: profile.Id,
                                            CallerType: ToolCallerType.Chat,
                                            ExecutionSourceId: assistantMsgId,
                                            InputArguments: call.Arguments,
                                            ResponsePayload: execResult.ResponsePayload,
                                            StatusCode: execResult.StatusCode,
                                            DurationMs: execResult.DurationMs,
                                            IsSuccess: execResult.IsSuccess,
                                            ErrorMessage: execResult.ErrorMessage), ct);
                                    }
                                }
                                catch
                                {
                                    // Bỏ qua lỗi ghi log để không gián đoạn luồng chat
                                }

                                conversationPayload.Add(new ChatMessagePayload(
                                    Role: "tool",
                                    Content: toolOutput,
                                    ToolCallId: call.Id));
                            }

                            await dbContext.SaveChangesAsync(ct);
                            // Tiếp tục vòng lặp gửi tool result ngược lại cho LLM
                        }
                    }
                    else
                    {
                        // LLM đã hoàn tất câu trả lời văn bản cuối cùng
                        var finalContent = response.Content ?? string.Empty;
                        var finalMsgId = Guid.CreateVersion7();

                        var finalMessage = new ChatMessage(
                            userMessage.ChannelId,
                            member.Id,
                            member.DisplayName,
                            isAgent: true,
                            content: finalContent,
                            callDepth: userMessage.CallDepth + 1,
                            parentMessageId: userMessage.Id,
                            id: finalMsgId);

                        dbContext.Messages.Add(finalMessage);
                        await dbContext.SaveChangesAsync(ct);

                        await hubContext.Clients.Group(channelGroup)
                            .SendAsync("ReceiveMessageChunk", finalMsgId, member.Id, finalContent, ct);
                        await hubContext.Clients.Group(channelGroup)
                            .SendAsync("ReceiveMessageCompleted", finalMsgId, member.Id, member.DisplayName, finalContent, ct);

                        completed = true;
                        return;
                    }
                }

                // Nếu chạm giới hạn MaxToolIterations mà LLM vẫn tiếp tục yêu cầu tool calls
                if (!completed)
                {
                    var maxIterMsgId = Guid.CreateVersion7();
                    const string maxIterWarning = "[Hệ thống: Đã đạt số lần gọi công cụ tối đa (5 bước lặp). Tác vụ tạm dừng để tránh vòng lặp vô hạn.]";

                    var warningMessage = new ChatMessage(
                        userMessage.ChannelId,
                        member.Id,
                        member.DisplayName,
                        isAgent: true,
                        content: maxIterWarning,
                        callDepth: userMessage.CallDepth + 1,
                        parentMessageId: userMessage.Id,
                        id: maxIterMsgId);

                    dbContext.Messages.Add(warningMessage);
                    await dbContext.SaveChangesAsync(ct);

                    await hubContext.Clients.Group(channelGroup)
                        .SendAsync("ReceiveMessageChunk", maxIterMsgId, member.Id, maxIterWarning, ct);
                    await hubContext.Clients.Group(channelGroup)
                        .SendAsync("ReceiveMessageCompleted", maxIterMsgId, member.Id, member.DisplayName, maxIterWarning, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex,
                    "Lỗi khi xử lý phản hồi Agent {MemberId} trong kênh {ChannelId}",
                    member.Id,
                    userMessage.ChannelId);

                const string errorMessage = "[Hệ thống: Không thể hoàn tất câu trả lời do sự cố kết nối với AI Gateway.]";

                try
                {
                    await hubContext.Clients
                        .Group(channelGroup)
                        .SendAsync("ReceiveMessageCompleted", agentMessageId, member.Id, member.DisplayName, errorMessage, CancellationToken.None);
                }
                catch (Exception sendEx)
                {
                    logger?.LogWarning(sendEx, "Không thể gửi ReceiveMessageCompleted khi xử lý lỗi stream");
                }

                try
                {
                    var errorSaveCommand = new SaveChatMessageCommand(
                        ChannelId: userMessage.ChannelId,
                        SenderMemberId: member.Id,
                        SenderDisplayName: member.DisplayName,
                        IsAgent: true,
                        Content: errorMessage,
                        CallDepth: userMessage.CallDepth + 1,
                        ParentMessageId: userMessage.Id,
                        MentionIds: null,
                        MessageId: agentMessageId);

                    await sender.Send(errorSaveCommand, CancellationToken.None);
                }
                catch (Exception saveEx)
                {
                    logger?.LogWarning(saveEx, "Không thể lưu thông điệp lỗi của Agent vào DB");
                }
            }
        }

        private async Task<RestApiExecutionResult> ExecuteToolCallAsync(
            List<ToolDefinitionDto> activeTools,
            LlmToolCallPayload toolCall,
            CancellationToken ct)
        {
            var toolDef = activeTools.FirstOrDefault(t => t.Name.Equals(toolCall.Name, StringComparison.OrdinalIgnoreCase));
            if (toolDef == null || restApiExecutor == null)
            {
                return RestApiExecutionResult.Failure($"Tool '{toolCall.Name}' is not available or executor is not configured.");
            }

            var headers = SecretMaskingHelper.ParseHeaders(toolDef.HeadersConfig);
            IDictionary<string, object?>? arguments = null;
            if (!string.IsNullOrWhiteSpace(toolCall.Arguments))
            {
                try
                {
                    arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.Arguments);
                }
                catch (JsonException)
                {
                    arguments = new Dictionary<string, object?>();
                }
            }

            var req = new RestApiExecutionRequest
            {
                Url = toolDef.EndpointUrl,
                Method = toolDef.HttpMethod,
                Headers = headers,
                Arguments = arguments,
                TimeoutSeconds = toolDef.TimeoutSeconds
            };

            return await restApiExecutor.ExecuteAsync(req, ct);
        }
    }
}
