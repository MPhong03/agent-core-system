using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentCoreSystem.Module.Agents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Agents_Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agent");

            migrationBuilder.CreateTable(
                name: "Tools",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HeadersConfig = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParametersSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentToolAccesses",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentToolAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentToolAccesses_Tools_ToolDefinitionId",
                        column: x => x.ToolDefinitionId,
                        principalSchema: "agent",
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolExecutionLogs",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallerType = table.Column<int>(type: "int", nullable: false),
                    ExecutionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputArguments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolExecutionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolExecutionLogs_Tools_ToolDefinitionId",
                        column: x => x.ToolDefinitionId,
                        principalSchema: "agent",
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolAccesses_AgentProfileId_ToolDefinitionId",
                schema: "agent",
                table: "AgentToolAccesses",
                columns: new[] { "AgentProfileId", "ToolDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentToolAccesses_ToolDefinitionId",
                schema: "agent",
                table: "AgentToolAccesses",
                column: "ToolDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutionLogs_AgentProfileId",
                schema: "agent",
                table: "ToolExecutionLogs",
                column: "AgentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutionLogs_ExecutionSourceId",
                schema: "agent",
                table: "ToolExecutionLogs",
                column: "ExecutionSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutionLogs_ToolDefinitionId",
                schema: "agent",
                table: "ToolExecutionLogs",
                column: "ToolDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Name",
                schema: "agent",
                table: "Tools",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentToolAccesses",
                schema: "agent");

            migrationBuilder.DropTable(
                name: "ToolExecutionLogs",
                schema: "agent");

            migrationBuilder.DropTable(
                name: "Tools",
                schema: "agent");
        }
    }
}
