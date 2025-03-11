using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TgBotYandexMar.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "MinPrice");

            migrationBuilder.AddColumn<string>(
                name: "AvgPrice",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaxPrice",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OAuthToken",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OAuthTokenExpiresAt",
                table: "Channels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceType",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RedirectUrl",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChannelStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChannelId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelStats_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelStats_ChannelId",
                table: "ChannelStats",
                column: "ChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelStats");

            migrationBuilder.DropColumn(
                name: "AvgPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "OAuthToken",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "OAuthTokenExpiresAt",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "PriceType",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "RedirectUrl",
                table: "Channels");

            migrationBuilder.RenameColumn(
                name: "MinPrice",
                table: "Products",
                newName: "Price");
        }
    }
}
