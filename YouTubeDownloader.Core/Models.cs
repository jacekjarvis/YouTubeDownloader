namespace YouTubeDownloader.Core;

/// <summary>Whether the user wants the video (with audio) or audio-only.</summary>
public enum MediaKind
{
    Video,
    Audio
}

/// <summary>A single video resolved from a URL (either a standalone video or one entry of a playlist).</summary>
public sealed class VideoEntry
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ThumbnailUrl { get; init; }

    public string DurationText =>
        Duration is { } d ? (d.TotalHours >= 1 ? d.ToString(@"h\:mm\:ss") : d.ToString(@"m\:ss")) : "—";
}

/// <summary>The result of resolving a URL: one video, or a whole playlist.</summary>
public sealed class ResolvedUrl
{
    public required bool IsPlaylist { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<VideoEntry> Videos { get; init; }
}

/// <summary>
/// A selectable quality. Because a playlist's videos each expose different streams,
/// a quality is expressed as a *target* (height for video, bitrate for audio) and the
/// closest available stream is matched per video at download time.
/// </summary>
public sealed class StreamOption
{
    public required MediaKind Kind { get; init; }
    public required string Label { get; init; }
    public bool IsHighest { get; init; }
    public int TargetHeight { get; init; }   // video: pick the stream closest to this height
    public long TargetBitrate { get; init; } // audio: pick the stream closest to this bitrate

    public override string ToString() => Label;
}
