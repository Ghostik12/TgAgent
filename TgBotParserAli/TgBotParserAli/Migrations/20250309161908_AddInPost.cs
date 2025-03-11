using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgBotParserAli.Migrations
{
    /// <inheritdoc />
    public partial class AddInPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Order",
                table: "PostSetting",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "PostSetting");
        }
    }
}
