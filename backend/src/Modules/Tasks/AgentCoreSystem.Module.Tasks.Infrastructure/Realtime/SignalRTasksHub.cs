// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AgentCoreSystem.Module.Tasks.Infrastructure.Realtime
{
    /// <summary>
    /// SignalR Hub quản lý cập nhật sự kiện bảng Kanban thời gian thực theo từng kênh làm việc.
    /// </summary>
    /// <remarks>
    /// Ánh xạ tại endpoint <c>/hubs/tasks</c>, phân nhóm theo <c>tasks_channel_{channelId}</c>.
    /// </remarks>
    [Authorize]
    public class SignalRTasksHub(ILogger<SignalRTasksHub>? logger = null) : Hub
    {
        /// <summary>
        /// Tham gia vào nhóm theo dõi bảng Kanban của kênh.
        /// </summary>
        public async Task JoinChannelTasks(Guid channelId)
        {
            var groupName = $"tasks_channel_{channelId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            logger?.LogDebug("Connection {ConnectionId} đã tham gia {GroupName}", Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Rời khỏi nhóm theo dõi bảng Kanban của kênh.
        /// </summary>
        public async Task LeaveChannelTasks(Guid channelId)
        {
            var groupName = $"tasks_channel_{channelId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            logger?.LogDebug("Connection {ConnectionId} đã rời {GroupName}", Context.ConnectionId, groupName);
        }
    }
}
