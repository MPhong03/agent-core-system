using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentCoreSystem.Module.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToolExecutionModeToAgentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ToolExecutionMode",
                schema: "org",
                table: "AgentProfiles",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolExecutionMode",
                schema: "org",
                table: "AgentProfiles");
        }
    }
}
