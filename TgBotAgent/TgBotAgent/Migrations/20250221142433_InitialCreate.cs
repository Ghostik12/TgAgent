using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgBotAgent.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "ChatId",
                value: 313064453L);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ChatId", "Username" },
                values: new object[] { 313064453L, "imkr1stal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "ChatId",
                value: 1451999567L);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ChatId", "Username" },
                values: new object[] { 987654321L, "sto" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ChatId", "Username" },
                values: new object[] { 4, 123456789L, "pro" });
        }
    }
}
