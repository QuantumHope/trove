using Microsoft.EntityFrameworkCore;
using Trove.Core.Playthroughs;

namespace Trove.Web.Data;

/// <summary>Trove's tables, in their own <c>trove</c> schema in CORA's Postgres.</summary>
public sealed class TroveDbContext(DbContextOptions<TroveDbContext> options) : DbContext(options)
{
    public const string Schema = "trove";

    public DbSet<YouTubeChannelRow> YouTubeChannels => Set<YouTubeChannelRow>();

    public DbSet<YouTubeVideoRow> YouTubeVideos => Set<YouTubeVideoRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<YouTubeChannelRow>(channel =>
        {
            channel.ToTable("youtube_channels");
            channel.HasKey(c => c.ChannelId);
            channel.Property(c => c.ChannelId).HasMaxLength(64);
            channel.Property(c => c.Handle).HasMaxLength(100);
            channel.HasIndex(c => c.Handle).IsUnique();
            channel.Property(c => c.Title).HasMaxLength(200);
            channel.Property(c => c.UploadsPlaylistId).HasMaxLength(64);
        });

        modelBuilder.Entity<YouTubeVideoRow>(video =>
        {
            video.ToTable("youtube_videos");
            video.HasKey(v => v.VideoId);
            video.Property(v => v.VideoId).HasMaxLength(32);
            video.Property(v => v.ChannelId).HasMaxLength(64);
            video.HasIndex(v => v.ChannelId);
            video.Property(v => v.Title).HasMaxLength(300);
            video.Property(v => v.ChannelTitle).HasMaxLength(200);
            video.Property(v => v.ThumbnailUrl).HasMaxLength(500);
            video.Property(v => v.State).HasConversion<string>().HasMaxLength(20);
            video.Property(v => v.Rating).HasPrecision(3, 2);
        });
    }
}

public sealed class YouTubeChannelRow
{
    public required string ChannelId { get; set; }

    /// <summary>The handle from settings, like <c>@MKIceAndFire</c>.</summary>
    public required string Handle { get; set; }

    public required string Title { get; set; }

    public required string UploadsPlaylistId { get; set; }

    /// <summary>The newest upload already read, so later syncs stop there instead of paging the whole channel.</summary>
    public DateTimeOffset? NewestUploadSeen { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }
}

public sealed class YouTubeVideoRow
{
    public required string VideoId { get; set; }

    public required string ChannelId { get; set; }

    public required string ChannelTitle { get; set; }

    public required string Title { get; set; }

    public DateTimeOffset PublishedAt { get; set; }

    public int DurationSeconds { get; set; }

    public required string ThumbnailUrl { get; set; }

    public long? ViewCount { get; set; }

    public WatchState State { get; set; }

    public decimal? Rating { get; set; }

    public DateOnly? WatchedOn { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public Playthrough ToPlaythrough() => new(
        VideoId, Title, ChannelTitle, PublishedAt, TimeSpan.FromSeconds(DurationSeconds), ThumbnailUrl, ViewCount, State,
        Rating, WatchedOn);
}
