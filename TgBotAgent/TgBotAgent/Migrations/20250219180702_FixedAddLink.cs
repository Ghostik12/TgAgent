using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgBotAgent.Migrations
{
    /// <inheritdoc />
    public partial class FixedAddLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserName1",
                table: "UserLinks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName2",
                table: "UserLinks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "UserLinks",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "UserName1", "UserName2" },
                values: new object[] { "unknown", "unknown" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName1",
                table: "UserLinks");

            migrationBuilder.DropColumn(
                name: "UserName2",
                table: "UserLinks");
        }
    }
}
