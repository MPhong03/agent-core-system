// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.Commands;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Infrastructure.Hubs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.DTOs;
using AgentCoreSystem.Module.Organization.Application.AgentProfiles.Queries.GetAgentProfiles;
using AgentCoreSystem.Module.Organization.Application.Members.DTOs;
using AgentCoreSystem.Module.Organization.Application.Members.Queries.GetChannelMembers;
using AgentCoreSystem.Module.Organization.Domain.Enums;
using AgentCoreSystem.Shared.Infrastructure.LLM;
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
    /// <para><strong>What:</strong> Xếp hàng tuần tự xử lý Agent, kiểm soát độ sâu CallDepth (ngắt khi >= 3), truy vấn sliding window 20 tin nhắn gần nhất và phát trực tiếp tokens ra SignalR group.</para>
    /// <para><strong>When:</strong> Được gọi từ SignalRChatHub hoặc API khi tin nhắn có gắn thẻ nhân sự AI.</para>
    /// <para><strong>Why:</strong> Đảm bảo luồng hội thoại đa tác nhân mượt mà, ngăn ngừa đệ quy vô hạn và đồng bộ dữ liệu vào ChatDbContext.</para>
    /// </remarks>
    public class MessageDispatcherService(
        IChatDbContext dbContext,
        ISender sender,
        ILLMProviderFactory llmProviderFactory,
        IHubContext<SignalRChatHub> hubContext,
        ILogger<MessageDispatcherService>? logger = null) : IMessageDispatcherService
    {
        private const int MaxCallDepth = 3;
        private const int SlidingWindowSize = 20;

        /// <summary>
        /// Điều phối tin nhắn tới các Agent được nhắc tên trong kênh làm việc theo hàng đợi tuần tự.
        /// </summary>
        /// <param name="userMessage">Tin nhắn kích hoạt việc điều phối.</param>
        /// <param name="mentionedAgentIds">Danh sách mã định danh thành viên hoặc hồ sơ Agent được gắn thẻ nhắc tên.</param>
        /// <param name="ct">Mã hủy bỏ tác vụ.</param>
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

        private async Task ProcessAgentResponseAsync(
            ChatMessageDto userMessage,
            MemberDto member,
            AgentProfileDto profile,
            CancellationToken ct)
        {
            var channelGroup = $"channel_{userMessage.ChannelId}";

            // 1. Truy vấn 20 tin nhắn gần nhất làm Sliding Window Context
            var recentMessages = await dbContext.Messages
                .AsNoTracking()
                .Where(m => m.ChannelId == userMessage.ChannelId)
                .OrderByDescending(m => m.CreatedDate)
                .Take(SlidingWindowSize)
                .ToListAsync(ct);

            var orderedHistory = recentMessages
                .OrderBy(m => m.CreatedDate)
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

            var agentMessageId = Guid.CreateVersion7();
            var fullContentBuilder = new StringBuilder();

            // 4. Stream từng chunk ra SignalR Group
            await foreach (var chunk in llmClient.StreamChatAsync(profile.ModelName, payload, profile.Temperature, ct))
            {
                fullContentBuilder.Append(chunk);

                await hubContext.Clients
                    .Group(channelGroup)
                    .SendAsync("ReceiveMessageChunk", agentMessageId, member.Id, chunk, ct);
            }

            var fullContent = fullContentBuilder.ToString();

            // 5. Lưu câu trả lời vào DB với CallDepth = userMessage.CallDepth + 1
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

            // 6. Phát sóng sự kiện hoàn thành phản hồi
            await hubContext.Clients
                .Group(channelGroup)
                .SendAsync("ReceiveMessageCompleted", agentMessageId, member.Id, fullContent, ct);
        }
    }
}
