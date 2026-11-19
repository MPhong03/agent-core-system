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
using AgentCoreSystem.Shared.Application.Messaging;
using AgentCoreSystem.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace AgentCoreSystem.Module.Chat.Application.Messages.Queries
{
    /// <summary>
    /// Handler xử lý truy vấn danh sách tin nhắn trong kênh làm việc.
    /// </summary>
    public class GetChannelMessagesQueryHandler(IChatDbContext dbContext)
        : IQueryHandler<GetChannelMessagesQuery, IReadOnlyList<ChatMessageDto>>
    {
        public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(
            GetChannelMessagesQuery request,
            CancellationToken cancellationToken)
        {
            var limit = request.Limit > 0 ? request.Limit : 50;

            var query = dbContext.Messages
                .AsNoTracking()
                .Include(m => m.Mentions)
                .Where(m => m.ChannelId == request.ChannelId);

            if (request.BeforeMessageId.HasValue)
            {
                var beforeMessage = await dbContext.Messages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.Id == request.BeforeMessageId.Value && m.ChannelId == request.ChannelId,
                        cancellationToken);

                if (beforeMessage is null)
                {
                    return Result.Failure<IReadOnlyList<ChatMessageDto>>(Error.NotFound(
                        "ChatMessage.NotFound",
                        $"Không tìm thấy tin nhắn mốc '{request.BeforeMessageId.Value}' trong kênh '{request.ChannelId}'."));
                }

                query = query.Where(m => m.CreatedDate < beforeMessage.CreatedDate);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedDate)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var dtoList = messages
                .OrderBy(m => m.CreatedDate)
                .Select(m => new ChatMessageDto(
                    m.Id,
                    m.ChannelId,
                    m.SenderMemberId,
                    m.SenderDisplayName,
                    m.IsAgent,
                    m.Content,
                    m.CallDepth,
                    m.ParentMessageId,
                    m.Mentions.Select(mention => mention.MentionedMemberId).ToList(),
                    m.CreatedDate))
                .ToList();

            return Result.Success<IReadOnlyList<ChatMessageDto>>(dtoList);
        }
    }
}
