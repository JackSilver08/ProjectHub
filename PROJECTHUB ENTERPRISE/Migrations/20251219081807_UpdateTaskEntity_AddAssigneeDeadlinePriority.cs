using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECTHUB_ENTERPRISE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTaskEntity_AddAssigneeDeadlinePriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "creator_id",
                table: "tasks",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "assignee_id",
                table: "tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "deadline",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_pinned",
                table: "tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "description", table: "tasks");
            migrationBuilder.DropColumn(name: "creator_id", table: "tasks");
            migrationBuilder.DropColumn(name: "assignee_id", table: "tasks");
            migrationBuilder.DropColumn(name: "priority", table: "tasks");
            migrationBuilder.DropColumn(name: "deadline", table: "tasks");
            migrationBuilder.DropColumn(name: "is_private", table: "tasks");
            migrationBuilder.DropColumn(name: "is_pinned", table: "tasks");
            migrationBuilder.DropColumn(name: "updated_at", table: "tasks");
        }

    }
}
