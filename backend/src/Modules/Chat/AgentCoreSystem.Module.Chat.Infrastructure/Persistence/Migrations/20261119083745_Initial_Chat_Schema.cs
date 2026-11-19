using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentCoreSystem.Module.Chat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Chat_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsAgent = table.Column<bool>(type: "bit", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallDepth = table.Column<int>(type: "int", nullable: false),
                    ParentMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageMentions",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentionedMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageMentions_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "chat",
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChannelId_CreatedDate",
                schema: "chat",
                table: "ChatMessages",
                columns: new[] { "ChannelId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageMentions_MentionedMemberId",
                schema: "chat",
                table: "MessageMentions",
                column: "MentionedMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageMentions_MessageId",
                schema: "chat",
                table: "MessageMentions",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageMentions",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "ChatMessages",
                schema: "chat");
        }
    }
}
