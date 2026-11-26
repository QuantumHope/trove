using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trove.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYouTubePlaythroughs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "trove");

            migrationBuilder.CreateTable(
                name: "youtube_channels",
                schema: "trove",
                columns: table => new
                {
                    ChannelId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Handle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UploadsPlaylistId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NewestUploadSeen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_channels", x => x.ChannelId);
                });

            migrationBuilder.CreateTable(
                name: "youtube_videos",
                schema: "trove",
                columns: table => new
                {
                    VideoId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChannelTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ViewCount = table.Column<long>(type: "bigint", nullable: true),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    WatchedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_youtube_videos", x => x.VideoId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_youtube_channels_Handle",
                schema: "trove",
                table: "youtube_channels",
                column: "Handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_youtube_videos_ChannelId",
                schema: "trove",
                table: "youtube_videos",
                column: "ChannelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "youtube_channels",
                schema: "trove");

            migrationBuilder.DropTable(
                name: "youtube_videos",
                schema: "trove");
        }
    }
}
