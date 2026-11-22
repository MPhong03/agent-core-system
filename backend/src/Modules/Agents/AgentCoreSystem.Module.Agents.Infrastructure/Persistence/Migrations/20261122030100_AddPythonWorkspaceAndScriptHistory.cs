using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentCoreSystem.Module.Agents.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPythonWorkspaceAndScriptHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentWorkspaceConfigs",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PythonVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MaxTimeoutSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 60),
                    IsVenvReady = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkspaceConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScriptExecutionHistories",
                schema: "agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallerType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExecutionSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScriptName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ScriptContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExitCode = table.Column<int>(type: "int", nullable: false),
                    Stdout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stderr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IsTimeout = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArtifactsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptExecutionHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaceConfigs_AgentProfileId",
                schema: "agent",
                table: "AgentWorkspaceConfigs",
                column: "AgentProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScriptExecutionHistories_AgentProfileId",
                schema: "agent",
                table: "ScriptExecutionHistories",
                column: "AgentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptExecutionHistories_ExecutionSourceId",
                schema: "agent",
                table: "ScriptExecutionHistories",
                column: "ExecutionSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentWorkspaceConfigs",
                schema: "agent");

            migrationBuilder.DropTable(
                name: "ScriptExecutionHistories",
                schema: "agent");
        }
    }
}
