using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TgBotAgent.Migrations
{
    /// <inheritdoc />
    public partial class SeedData2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "Id", "ChatId" },
                values: new object[] { 1, 1451999567L });

            migrationBuilder.InsertData(
                table: "BlacklistWords",
                columns: new[] { "Id", "Word" },
                values: new object[] { 1, "спам" });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "Id", "FromUserId", "Text", "Timestamp", "ToUserId" },
                values: new object[,]
                {
                    { 1, 123456789L, "Привет!", new DateTime(2025, 2, 17, 10, 0, 0, 0, DateTimeKind.Utc), 987654321L },
                    { 2, 987654321L, "Как дела?", new DateTime(2023, 10, 1, 10, 0, 0, 0, DateTimeKind.Utc), 123456789L }
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[] { 1, "WelcomeMessage", "Добро пожаловать!" });

            migrationBuilder.InsertData(
                table: "UserLinks",
                columns: new[] { "Id", "UserId1", "UserId2" },
                values: new object[] { 1, 123456789L, 987654321L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BlacklistWords",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Settings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "UserLinks",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
