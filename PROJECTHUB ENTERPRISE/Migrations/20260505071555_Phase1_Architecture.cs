using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECTHUB_ENTERPRISE.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Architecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "contributes_to_progress",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contributes_to_progress",
                table: "tasks");
        }
    }
}
