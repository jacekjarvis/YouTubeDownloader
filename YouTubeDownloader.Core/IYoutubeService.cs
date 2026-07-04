namespace YouTubeDownloader.Core;

public interface IYoutubeService
{
    /// <summary>Resolves a URL into a single video or a full playlist.</summary>
    Task<ResolvedUrl> ResolveAsync(string url, CancellationToken ct = default);

    /// <summary>Returns the available quality options for a video, highest quality/size first.</summary>
    Task<IReadOnlyList<StreamOption>> GetStreamOptionsAsync(string videoUrl, MediaKind kind, CancellationToken ct = default);

    /// <summary>
    /// Downloads a single video using the closest stream matching <paramref name="option"/>,
    /// reporting 0.0–1.0 progress. Returns the saved file path.
    /// </summary>
    Task<string> DownloadAsync(
        VideoEntry video,
        MediaKind kind,
        StreamOption option,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads only the raw audio container for a video (no mp3 conversion), reporting
    /// 0.0–1.0 progress. Returns the container file path, ready to hand to <see cref="ConvertToMp3"/>.
    /// Lets a caller keep downloading while conversions run on a separate worker.
    /// </summary>
    Task<string> DownloadAudioContainerAsync(
        VideoEntry video,
        StreamOption option,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Converts a downloaded audio container to mp3. This is a blocking, CPU-bound call —
    /// run it off the UI thread. Returns the final mp3 path, or the original container path on failure.
    /// </summary>
    string ConvertToMp3(string containerPath);
}
