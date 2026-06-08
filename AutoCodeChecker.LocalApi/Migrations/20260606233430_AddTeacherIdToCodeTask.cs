using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoCodeChecker.LocalApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherIdToCodeTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Tasks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TeacherId",
                table: "Tasks",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_TeacherId",
                table: "Tasks",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Users_TeacherId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_TeacherId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Tasks");
        }
    }
}
