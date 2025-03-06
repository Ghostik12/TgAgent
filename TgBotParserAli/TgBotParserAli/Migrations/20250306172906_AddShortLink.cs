using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgBotParserAli.Migrations
{
    /// <inheritdoc />
    public partial class AddShortLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseShortLinks",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseShortLinks",
                table: "Channels");
        }
    }
}
