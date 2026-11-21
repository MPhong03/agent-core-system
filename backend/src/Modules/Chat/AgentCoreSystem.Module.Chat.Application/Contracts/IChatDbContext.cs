// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Chat.Application.Contracts
{
    /// <summary>
    /// Giao diện DbContext cho phân hệ Chat.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Cung cấp các DbSet thực thể tin nhắn trò chuyện và lượt nhắc tên (@mention).</para>
    /// <para><strong>Why:</strong> Tuân thủ Clean Architecture, cho phép Application CQRS Handlers truy cập dữ liệu mà không bị phụ thuộc vào chi tiết tầng Infrastructure.</para>
    /// </remarks>
    public interface IChatDbContext
    {
        // ── DbSets ──

        /// <summary>
        /// Tập hợp các thực thể tin nhắn trò chuyện trong kênh.
        /// </summary>
        DbSet<ChatMessage> Messages { get; }

        /// <summary>
        /// Tập hợp các thực thể tin nhắn trò chuyện trong kênh (tên định danh thay thế).
        /// </summary>
        DbSet<ChatMessage> ChatMessages { get; }

        /// <summary>
        /// Tập hợp các lượt nhắc tên thành viên trong tin nhắn.
        /// </summary>
        DbSet<MessageMention> Mentions { get; }

        /// <summary>
        /// Tập hợp các lượt nhắc tên thành viên trong tin nhắn (tên định danh thay thế).
        /// </summary>
        DbSet<MessageMention> MessageMentions { get; }

        /// <summary>
        /// Tập hợp các yêu cầu gọi công cụ đang chờ xác nhận trong kênh.
        /// </summary>
        DbSet<PendingToolCall> PendingToolCalls { get; }

        // ── Persistence Operations ──

        /// <summary>
        /// Lưu các thay đổi vào cơ sở dữ liệu trên schema 'chat'.
        /// </summary>
        /// <param name="cancellationToken">Mã hủy bỏ tác vụ không đồng bộ.</param>
        /// <returns>Số lượng bản ghi bị tác động trong cơ sở dữ liệu.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
