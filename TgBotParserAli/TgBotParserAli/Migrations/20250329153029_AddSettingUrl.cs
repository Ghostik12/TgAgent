using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgBotParserAli.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlTemplate",
                table: "PostSetting",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Tokens",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AccessToken", "ExpiresAt", "RefreshToken" },
                values: new object[] { "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzX3Rva2VuIiwiZXhwIjoxNzQyNTAwNDE3LCJ1c2VyX2lkIjo2OTQzNzksInVzZXJfcm9sZSI6InVzZXIiLCJjbGllbnRfcGxhdGZvcm0iOiJ3ZWIiLCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNsaWVudF9pZCI6IjVDQmhqWTBQbXdxRzRFc2M5THlsSE9OdlNKdVVUN2lnIiwiY2hlY2tfaXAiOmZhbHNlLCJ0b2tlbiI6IjQ4NzFlZTcyZDhhNGU1OThlN2U1NDBkZWM0OTJhNjQwYWQyZmU0ZTIiLCJzY29wZSI6InVzZXJfaXNzdWVkX3Rva2VuIn0.mPDj2SyGctiRI1JVXE3w6cqm-EW4A2Cd-w-J8EabAlNExGAbNfGIquKtOiP-W_kQJMBKuCnIq3SUfMALBsjo9A", new DateTime(2025, 3, 20, 22, 53, 37, 0, DateTimeKind.Utc), "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoicmVmcmVzaF90b2tlbiIsImV4cCI6MTc0MzcxMDAxNywidG9rZW4iOiI3MjU5MDdkYjVhNTlhOWUwMmZiNzdkYWE1ZTgyZTk3M2E0MWI5NmM4IiwidXNlcl9pZCI6Njk0Mzc5LCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNoZWNrX2lwIjpmYWxzZSwic2NvcGUiOiJ1c2VyX2lzc3VlZF90b2tlbiJ9.RxHuycPFS_oSZibp4TRdajaRH_8hrI43PyCHB-Qvpgnhn6BVWhrbE5q1w60UYsfzJQzW3QYtrJ1eYS4Iat8fjQ" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlTemplate",
                table: "PostSetting");

            migrationBuilder.UpdateData(
                table: "Tokens",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AccessToken", "ExpiresAt", "RefreshToken" },
                values: new object[] { "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoiYWNjZXNzX3Rva2VuIiwiZXhwIjoxNzQyMTMwMjE1LCJ1c2VyX2lkIjo2OTQzNzksInVzZXJfcm9sZSI6InVzZXIiLCJjbGllbnRfcGxhdGZvcm0iOiJ3ZWIiLCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNsaWVudF9pZCI6IlE2Z0tEdWkxRnQ1SWhIWWtjQ3MzbVdHanBiVkVvQVA5IiwiY2hlY2tfaXAiOmZhbHNlLCJ0b2tlbiI6ImI0ZjMwMmZiYzdmMjljNzIyNjQ4NWQ1MmNiODY1ZGU5ZDU1MGIwZjQiLCJzY29wZSI6InVzZXJfaXNzdWVkX3Rva2VuIn0.kP3DeEhD0dh1DuIU5Yn0rciPcnhpLdr4YQlnsI0EuCyFXGXY3MYudXC1xmtpmG3ETbwBqrLsDhEUvbv5JqThyw", new DateTime(2025, 3, 17, 0, 0, 0, 0, DateTimeKind.Utc), "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiJ9.eyJ0b2tlbl90eXBlIjoicmVmcmVzaF90b2tlbiIsImV4cCI6MTc0MzMzOTgxNSwidG9rZW4iOiIzOGU5ZDBkZjMzNzc3MmUzZWVkNTlmODJkNjFjMTM1NzQ2Y2NlNjM3IiwidXNlcl9pZCI6Njk0Mzc5LCJjbGllbnRfaXAiOiI5MS4yMzcuMTc5LjE4MSIsImNoZWNrX2lwIjpmYWxzZSwic2NvcGUiOiJ1c2VyX2lzc3VlZF90b2tlbiJ9.wHQ2g12n0pCdVm10uV_XvepIMxwqiJhsDUManKUfdOsv9XdcXTtF3sNUaduv3rcZFyxks7KOoAJ8m_c5VqFFUA" });
        }
    }
}
