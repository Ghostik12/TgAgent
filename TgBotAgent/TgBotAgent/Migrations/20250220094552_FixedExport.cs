using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TgBotAgent.Migrations
{
    /// <inheritdoc />
    public partial class FixedExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromUsername",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToUsername",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FromUsername", "ToUsername" },
                values: new object[] { "pro", "sto" });

            migrationBuilder.UpdateData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FromUsername", "ToUsername" },
                values: new object[] { "sto", "pro" });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "Id", "FromUserId", "FromUsername", "Text", "Timestamp", "ToUserId", "ToUsername" },
                values: new object[,]
                {
                    { 3, 7140034389L, "lercky", "Привет!", new DateTime(2025, 2, 19, 10, 0, 0, 0, DateTimeKind.Utc), 1952565657L, "violet_myst" },
                    { 4, 1952565657L, "violet_myst", "Как дела?", new DateTime(2025, 2, 19, 10, 1, 0, 0, DateTimeKind.Utc), 7140034389L, "lercky" }
                });

            migrationBuilder.InsertData(
                table: "UserLinks",
                columns: new[] { "Id", "UserId1", "UserId2", "UserName1", "UserName2" },
                values: new object[] { 2, 1952565657L, 7140034389L, "violet_myst", "lercky" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Messages",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "UserLinks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "FromUsername",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ToUsername",
                table: "Messages");
        }
    }
}
