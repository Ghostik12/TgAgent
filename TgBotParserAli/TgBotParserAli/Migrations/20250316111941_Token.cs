using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TgBotParserAli.Migrations
{
    /// <inheritdoc />
    public partial class Token : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tokens", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tokens",
                columns: new[] { "Id", "AccessToken", "ExpiresAt", "RefreshToken" },
                values: new object[] { 1, "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzX3Rva2VuIiwiZXhwIjoxNzQyMTMwMjE1LCJ1c2VyX2lkIjo2OTQzNzksInVzZXJfcm9sZSI6InVzZXIiLCJjbGllbnRfcGxhdGZvcm0iOiJ3ZWIiLCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNsaWVudF9pZCI6IlE2Z0tEdWkxRnQ1SWhIWWtjQ3MzbVdHanBiVkVvQVA5IiwiY2hlY2tfaXAiOmZhbHNlLCJ0b2tlbiI6ImI0ZjMwMmZiYzdmMjljNzIyNjQ4NWQ1MmNiODY1ZGU5ZDU1MGIwZjQiLCJzY29wZSI6InVzZXJfaXNzdWVkX3Rva2VuIn0.kP3DeEhD0dh1DuIU5Yn0rciPcnhpLdr4YQlnsI0EuCyFXGXY3MYudXC1xmtpmG3ETbwBqrLsDhEUvbv5JqThyw", new DateTime(2025, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoicmVmcmVzaF90b2tlbiIsImV4cCI6MTc0MzMzOTgxNSwidG9rZW4iOiIzOGU5ZDBkZjMzNzc3MmUzZWVkNTlmODJkNjFjMTM1NzQ2Y2NlNjM3IiwidXNlcl9pZCI6Njk0Mzc5LCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNoZWNrX2lwIjpmYWxzZSwic2NvcGUiOiJ1c2VyX2lzc3VlZF90b2tlbiJ9.wHQ2g12n0pCdVm10uV_XvepIMxwqiJhsDUManKUfdOsv9XdcXTtF3sNUaduv3rcZFyxks7KOoAJ8m_c5VqFFUA" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tokens");
        }
    }
}
