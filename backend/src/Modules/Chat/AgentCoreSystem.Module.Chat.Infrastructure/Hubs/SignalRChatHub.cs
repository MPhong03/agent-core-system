// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.Commands;
using AgentCoreSystem.Module.Organization.Application.Members.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Module.Chat.Infrastructure.Hubs
{
    /// <summary>
    /// SignalR Hub quản lý kết nối và điều phối tin nhắn thời gian thực trong các kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Quản lý SignalR groups theo mã kênh (<c>channel_{channelId}</c>), kiểm thực quyền thành viên kênh khi gửi tin, lưu trữ tin nhắn và điều phối @mentions tới các AI Agent.</para>
    /// <para><strong>When:</strong> Khách hàng (Web Client) kết nối tới endpoint <c>/hubs/chat</c> để nhận và gửi thông điệp trò chuyện thời gian thực.</para>
    /// <para><strong>Why:</strong> Cung cấp kênh truyền thông hai chiều (full-duplex) với độ trễ tối thiểu, hỗ trợ streaming từng token LLM từ AI Agent về giao diện người dùng.</para>
    /// </remarks>
    [Authorize]
    public class SignalRChatHub(
        ISender sender,
        IServiceScopeFactory scopeFactory,
        ILogger<SignalRChatHub>? logger = null) : Hub
    {
        /// <summary>
        /// Tham gia vào nhóm lắng nghe tin nhắn thời gian thực của một kênh làm việc cụ thể.
        /// </summary>
        /// <param name="channelId">Mã định danh kênh làm việc.</param>
        public async Task JoinChannel(Guid channelId)
        {
            var groupName = $"channel_{channelId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Rời khỏi nhóm lắng nghe tin nhắn của một kênh làm việc.
        /// </summary>
        /// <param name="channelId">Mã định danh kênh làm việc.</param>
        public async Task LeaveChannel(Guid channelId)
        {
            var groupName = $"channel_{channelId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Gửi một tin nhắn mới vào kênh làm việc, phát sóng tới các thành viên và kích hoạt @mention Agents.
        /// </summary>
        /// <param name="channelId">Mã định danh kênh làm việc.</param>
        /// <param name="content">Nội dung văn bản tin nhắn.</param>
        /// <param name="mentionIds">Danh sách mã định danh thành viên được gắn thẻ nhắc tên (@mention).</param>
        public async Task SendMessage(Guid channelId, string content, List<Guid>? mentionIds)
        {
            var userIdString = Context.UserIdentifier
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? (Context.Items.TryGetValue("UserId", out var itemVal) ? itemVal?.ToString() : null);

            if (!Guid.TryParse(userIdString, out var userId))
            {
                throw new HubException("Người dùng chưa được xác thực danh tính hợp lệ.");
            }

            // 1. Kiểm tra quyền thành viên trong kênh thông qua MediatR Query
            var memberResult = await sender.Send(new GetMemberByUserIdAndChannelQuery(userId, channelId));
            if (memberResult.IsFailure)
            {
                throw new HubException(memberResult.Error.Description);
            }

            var member = memberResult.Value;

            // 2. Lưu tin nhắn vào CSDL qua SaveChatMessageCommand
            var saveCommand = new SaveChatMessageCommand(
                ChannelId: channelId,
                SenderMemberId: member.Id,
                SenderDisplayName: member.DisplayName,
                IsAgent: false,
                Content: content,
                CallDepth: 0,
                ParentMessageId: null,
                MentionIds: mentionIds);

            var saveResult = await sender.Send(saveCommand);
            if (saveResult.IsFailure)
            {
                throw new HubException(saveResult.Error.Description);
            }

            var message = saveResult.Value;

            // 3. Phát sóng tin nhắn tới toàn bộ nhóm SignalR của kênh
            var groupName = $"channel_{channelId}";
            await Clients.Group(groupName).SendAsync("ReceiveMessage", message);

            // 4. Ủy thác điều phối @mention tới MessageDispatcherService trong background với scope riêng
            if (mentionIds is { Count: > 0 })
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var scopedDispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcherService>();
                        await scopedDispatcher.DispatchMessageAsync(message, mentionIds, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Lỗi xảy ra khi điều phối @mention cho tin nhắn {MessageId}", message.Id);
                    }
                });
            }
        }
    }
}
