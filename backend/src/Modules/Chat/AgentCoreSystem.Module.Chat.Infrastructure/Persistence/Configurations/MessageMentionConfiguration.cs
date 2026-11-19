// ------------------------------------------------------------------------------------------------
// Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------------

using AgentCoreSystem.Module.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgentCoreSystem.Module.Chat.Infrastructure.Persistence.Configurations
{
    public class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
    {
        public void Configure(EntityTypeBuilder<MessageMention> builder)
        {
            builder.ToTable("MessageMentions");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.MessageId)
                .IsRequired();

            builder.Property(m => m.MentionedMemberId)
                .IsRequired();

            builder.HasIndex(m => m.MessageId);
            builder.HasIndex(m => m.MentionedMemberId);

            builder.HasOne(m => m.Message)
                .WithMany(msg => msg.Mentions)
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(m => !m.IsDeleted);
        }
    }
}
