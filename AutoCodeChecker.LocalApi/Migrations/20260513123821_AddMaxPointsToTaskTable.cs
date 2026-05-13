using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoCodeChecker.LocalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxPointsToTaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPoints",
                table: "Tasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "Tasks");
        }
    }
}
