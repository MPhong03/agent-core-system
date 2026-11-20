// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentCoreSystem.Module.Chat.Application.Contracts;
using AgentCoreSystem.Module.Chat.Application.Messages.DTOs;
using AgentCoreSystem.Module.Chat.Domain.Entities;
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using FluentValidation;

namespace AgentCoreSystem.Module.Chat.Application.Messages.Commands
{
    /// <summary>
    /// Lệnh lưu một tin nhắn trò chuyện mới vào kênh làm việc.
    /// </summary>
    /// <remarks>
    /// <para><strong>What:</strong> Lưu trữ bản ghi ChatMessage cùng các lượt nhắc tên (@mention) tương ứng.</para>
    /// <para><strong>Why:</strong> Được gọi từ SignalR ChatHub khi Người dùng hoặc Agent phát đi tin nhắn trong kênh.</para>
    /// </remarks>
    /// <param name="ChannelId">Mã định danh kênh trò chuyện.</param>
    /// <param name="SenderMemberId">Mã định danh thành viên gửi tin.</param>
    /// <param name="SenderDisplayName">Tên hiển thị của người gửi tại thời điểm gửi tin.</param>
    /// <param name="IsAgent">Xác định tin nhắn được gửi bởi AI Agent hay Người dùng.</param>
    /// <param name="Content">Nội dung văn bản của tin nhắn.</param>
    /// <param name="CallDepth">Độ sâu cuộc gọi phản hồi tự động (mặc định 0).</param>
    /// <param name="ParentMessageId">Mã tin nhắn cha được phản hồi (nếu có).</param>
    /// <param name="MentionIds">Danh sách mã thành viên được nhắc tên trong tin nhắn (nếu có).</param>
    /// <param name="MessageId">Mã định danh tin nhắn được cấp trước (UUIDv7, nếu có).</param>
    public record SaveChatMessageCommand(
        Guid ChannelId,
        Guid SenderMemberId,
        string SenderDisplayName,
        bool IsAgent,
        string Content,
        int CallDepth = 0,
        Guid? ParentMessageId = null,
        List<Guid>? MentionIds = null,
        Guid? MessageId = null) : ICommand<ChatMessageDto>;

    /// <summary>
    /// Bộ xác thực tính hợp lệ của lệnh lưu tin nhắn trò chuyện.
    /// </summary>
    public class SaveChatMessageCommandValidator : AbstractValidator<SaveChatMessageCommand>
    {
        public SaveChatMessageCommandValidator()
        {
            RuleFor(x => x.ChannelId)
                .NotEmpty().WithMessage("Mã kênh trò chuyện không được để trống.");

            RuleFor(x => x.SenderMemberId)
                .NotEmpty().WithMessage("Mã thành viên gửi không được để trống.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung tin nhắn không được để trống.");

            RuleFor(x => x.CallDepth)
                .GreaterThanOrEqualTo(0).WithMessage("Độ sâu cuộc gọi không được là số âm.");
        }
    }

    /// <summary>
    /// Handler xử lý lưu tin nhắn trò chuyện vào cơ sở dữ liệu.
    /// </summary>
    /// <remarks>
    /// <para>Khởi tạo và lưu trữ bản ghi <see cref="ChatMessage"/> cùng các liên kết <see cref="MessageMention"/> vào schema 'chat'.</para>
    /// <para>AI-NOTE: Hỗ trợ truyền mã MessageId định sẵn (UUIDv7) để đồng bộ nhất quán giữa Client streaming và Database record.</para>
    /// </remarks>
    public class SaveChatMessageCommandHandler(IChatDbContext dbContext)
        : ICommandHandler<SaveChatMessageCommand, ChatMessageDto>
    {
        /// <inheritdoc />
        public async Task<Result<ChatMessageDto>> Handle(
            SaveChatMessageCommand request,
            CancellationToken cancellationToken)
        {
            var message = new ChatMessage(
                request.ChannelId,
                request.SenderMemberId,
                request.SenderDisplayName,
                request.IsAgent,
                request.Content,
                request.CallDepth,
                request.ParentMessageId,
                request.MessageId);

            if (request.MentionIds is { Count: > 0 })
            {
                foreach (var mentionId in request.MentionIds.Distinct())
                {
                    message.AddMention(mentionId);
                }
            }

            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync(cancellationToken);

            var dto = new ChatMessageDto(
                message.Id,
                message.ChannelId,
                message.SenderMemberId,
                message.SenderDisplayName,
                message.IsAgent,
                message.Content,
                message.CallDepth,
                message.ParentMessageId,
                message.Mentions.Select(m => m.MentionedMemberId).ToList(),
                message.CreatedDate);

            return Result.Success(dto);
        }
    }
}
