using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoCodeChecker.LocalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "Tasks");
        }
    }
}
