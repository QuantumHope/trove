using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Trove.Web.YouTube;

/// <summary>
/// The few YouTube Data API v3 calls Trove needs. Each costs one unit of the free 10,000 a day, and a page holds 50
/// videos, so reading a whole channel costs a few dozen units.
/// </summary>
public sealed class YouTubeDataApi(HttpClient http, YouTubeKeyStore keys)
{
    public static readonly Uri BaseAddress = new("https://www.googleapis.com/youtube/v3/");

    /// <summary>Looks a channel up by handle. <paramref name="apiKey"/> tries a key that isn't saved yet.</summary>
    public async Task<ChannelInfo?> FindChannelAsync(string handle, CancellationToken cancellationToken, string? apiKey = null)
    {
        var response = await GetAsync<ListResponse<ChannelItem>>(
            $"channels?part=snippet,contentDetails&forHandle={Uri.EscapeDataString(handle.TrimStart('@'))}", cancellationToken, apiKey);
        var channel = response.Items?.FirstOrDefault();
        return channel is null
            ? null
            : new ChannelInfo(channel.Id, channel.Snippet.Title, channel.ContentDetails.RelatedPlaylists.Uploads);
    }

    /// <summary>One page of a channel's uploads, newest first.</summary>
    public async Task<UploadsPage> GetUploadsAsync(string playlistId, string? pageToken, CancellationToken cancellationToken)
    {
        var query = $"playlistItems?part=snippet,contentDetails&maxResults=50&playlistId={Uri.EscapeDataString(playlistId)}";
        if (pageToken is not null)
        {
            query += $"&pageToken={Uri.EscapeDataString(pageToken)}";
        }

        var response = await GetAsync<ListResponse<PlaylistItem>>(query, cancellationToken);
        var uploads = (response.Items ?? [])
            .Select(item => new Upload(item.ContentDetails.VideoId, item.Snippet.Title, item.ContentDetails.VideoPublishedAt))
            .ToList();
        return new UploadsPage(uploads, response.NextPageToken);
    }

    /// <summary>Lengths, views and thumbnails for up to 50 videos. Live and upcoming streams are left out.</summary>
    public async Task<IReadOnlyList<VideoDetails>> GetVideosAsync(IReadOnlyCollection<string> videoIds, CancellationToken cancellationToken)
    {
        if (videoIds.Count == 0)
        {
            return [];
        }

        var ids = string.Join(',', videoIds.Select(Uri.EscapeDataString));
        var response = await GetAsync<ListResponse<VideoItem>>($"videos?part=snippet,contentDetails,statistics&id={ids}", cancellationToken);
        return (response.Items ?? [])
            .Where(v => v.Snippet.LiveBroadcastContent is null or "none")
            .Select(v => new VideoDetails(
                v.Id,
                v.Snippet.Title,
                v.Snippet.ChannelTitle,
                v.Snippet.PublishedAt,
                ParseDuration(v.ContentDetails.Duration),
                v.Snippet.Thumbnails.Best(),
                long.TryParse(v.Statistics?.ViewCount, out var views) ? views : null))
            .ToList();
    }

    /// <summary>YouTube gives lengths as ISO 8601 durations, like <c>PT10H4M31S</c> or <c>P1DT2H</c>.</summary>
    internal static TimeSpan ParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration))
        {
            return TimeSpan.Zero;
        }

        try
        {
            return XmlConvert.ToTimeSpan(duration);
        }
        catch (FormatException)
        {
            return TimeSpan.Zero;
        }
    }

    private async Task<T> GetAsync<T>(string relative, CancellationToken cancellationToken, string? apiKey = null)
    {
        var key = apiKey ?? (await keys.GetAsync(cancellationToken)).Key;
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new YouTubeApiException("No YouTube API key is set.");
        }

        // The key goes in a header, not the address, so request logging never writes it down.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseAddress, relative));
        request.Headers.Add("X-Goog-Api-Key", key);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new YouTubeApiException(response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "YouTube turned the request down. Check that the API key is right.",
                HttpStatusCode.Forbidden => "YouTube refused the key. Check that the YouTube Data API v3 is enabled for it, or the daily quota may be used up.",
                _ => $"YouTube answered {(int)response.StatusCode}.",
            });
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new YouTubeApiException("YouTube sent an empty answer.");
    }

    public sealed record ChannelInfo(string ChannelId, string Title, string UploadsPlaylistId);

    public sealed record Upload(string VideoId, string Title, DateTimeOffset? PublishedAt);

    public sealed record UploadsPage(IReadOnlyList<Upload> Uploads, string? NextPageToken);

    public sealed record VideoDetails(
        string VideoId, string Title, string ChannelTitle, DateTimeOffset PublishedAt, TimeSpan Duration, string ThumbnailUrl, long? ViewCount);

    private sealed record ListResponse<T>(List<T>? Items, string? NextPageToken);

    private sealed record ChannelItem(string Id, Snippet Snippet, ChannelContentDetails ContentDetails);

    private sealed record ChannelContentDetails(RelatedPlaylists RelatedPlaylists);

    private sealed record RelatedPlaylists(string Uploads);

    private sealed record PlaylistItem(Snippet Snippet, PlaylistItemContentDetails ContentDetails);

    private sealed record PlaylistItemContentDetails(string VideoId, DateTimeOffset? VideoPublishedAt);

    private sealed record VideoItem(string Id, Snippet Snippet, VideoContentDetails ContentDetails, VideoStatistics? Statistics);

    private sealed record VideoContentDetails(string? Duration);

    private sealed record VideoStatistics(string? ViewCount);

    private sealed record Snippet(string Title, string ChannelTitle, DateTimeOffset PublishedAt, string? LiveBroadcastContent, Thumbnails Thumbnails);

    private sealed record Thumbnails(Thumbnail? Default, Thumbnail? Medium, Thumbnail? High, Thumbnail? Standard, Thumbnail? Maxres)
    {
        // High is 480x360 with the 16:9 picture letterboxed inside; the card crops it to 16:9 (object-fit: cover), which
        // trims exactly the bars. Maxres would be sharper but is large and missing on older uploads.
        public string Best() => (High ?? Medium ?? Standard ?? Maxres ?? Default)?.Url ?? "";
    }

    private sealed record Thumbnail(string Url);
}

public sealed class YouTubeApiException(string message) : Exception(message);
