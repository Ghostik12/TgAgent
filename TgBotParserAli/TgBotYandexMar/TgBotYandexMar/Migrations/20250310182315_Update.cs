using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TgBotYandexMar.Migrations
{
    /// <inheritdoc />
    public partial class Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ChatId = table.Column<string>(type: "text", nullable: false),
                    ParseCount = table.Column<int>(type: "integer", nullable: false),
                    MaxPostsPerDay = table.Column<int>(type: "integer", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    Clid = table.Column<string>(type: "text", nullable: false),
                    UseExactMatch = table.Column<bool>(type: "boolean", nullable: false),
                    UseLowPrice = table.Column<bool>(type: "boolean", nullable: false),
                    ShowRating = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOpinionCount = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeywordSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Keyword = table.Column<string>(type: "text", nullable: false),
                    ParseFrequency = table.Column<TimeSpan>(type: "interval", nullable: false),
                    PostFrequency = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ChannelId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeywordSettings_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PriceTemplate = table.Column<string>(type: "text", nullable: false),
                    TitleTemplate = table.Column<string>(type: "text", nullable: false),
                    CaptionTemplate = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<string>(type: "text", nullable: false),
                    ShowRating = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOpinionCount = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSetting_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    OpinionCount = table.Column<int>(type: "integer", nullable: false),
                    IsPosted = table.Column<bool>(type: "boolean", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChannelId = table.Column<int>(type: "integer", nullable: false),
                    Keyword = table.Column<string>(type: "text", nullable: false),
                    Photos = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeywordStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParsedCount = table.Column<int>(type: "integer", nullable: false),
                    LastParsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    KeywordSettingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeywordStats_KeywordSettings_KeywordSettingId",
                        column: x => x.KeywordSettingId,
                        principalTable: "KeywordSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Admin",
                columns: new[] { "Id", "ChatId", "UserName" },
                values: new object[,]
                {
                    { 1, 1451999567L, "faust_harric" },
                    { 2, 292720339L, "admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordSettings_ChannelId",
                table: "KeywordSettings",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordStats_KeywordSettingId",
                table: "KeywordStats",
                column: "KeywordSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSetting_ChannelId",
                table: "PostSetting",
                column: "ChannelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ChannelId",
                table: "Products",
                column: "ChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "KeywordStats");

            migrationBuilder.DropTable(
                name: "PostSetting");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "KeywordSettings");

            migrationBuilder.DropTable(
                name: "Channels");
        }
    }
}
