using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YouTubeDownloader.Core;

public class YoutubeService : IYoutubeService
{
    private readonly YoutubeClient _youtube = new();
    private readonly string _ffmpegDirectory;

    /// <param name="ffmpegDirectory">
    /// Folder containing ffmpeg.exe. Defaults to an "ffmpeg" folder next to the running app.
    /// </param>
    public YoutubeService(string? ffmpegDirectory = null)
    {
        _ffmpegDirectory = ffmpegDirectory ?? Path.Combine(AppContext.BaseDirectory, "ffmpeg");
    }

    public async Task<ResolvedUrl> ResolveAsync(string url, CancellationToken ct = default)
    {
        url = (url ?? string.Empty).Trim();

        // A "watch?v=...&list=..." URL parses as both; treat the presence of a playlist as a playlist
        // so the whole list is offered (the UI lets the user deselect individual videos).
        if (PlaylistId.TryParse(url) is { } playlistId)
        {
            var playlist = await _youtube.Playlists.GetAsync(playlistId, ct);
            var videos = new List<VideoEntry>();
            await foreach (var v in _youtube.Playlists.GetVideosAsync(playlistId, ct))
            {
                videos.Add(ToEntry(v.Url, v.Title, v.Author.ChannelTitle, v.Duration, v.Thumbnails));
            }

            return new ResolvedUrl { IsPlaylist = true, Title = playlist.Title, Videos = videos };
        }

        if (VideoId.TryParse(url) is { } videoId)
        {
            var video = await _youtube.Videos.GetAsync(videoId, ct);
            var entry = ToEntry(video.Url, video.Title, video.Author.ChannelTitle, video.Duration, video.Thumbnails);
            return new ResolvedUrl { IsPlaylist = false, Title = video.Title, Videos = new[] { entry } };
        }

        throw new ArgumentException("That doesn't look like a YouTube video or playlist link.");
    }

    public async Task<IReadOnlyList<StreamOption>> GetStreamOptionsAsync(
        string videoUrl, MediaKind kind, CancellationToken ct = default)
    {
        var manifest = await _youtube.Videos.Streams.GetManifestAsync(videoUrl, ct);
        var options = new List<StreamOption>();

        if (kind == MediaKind.Video)
        {
            var streams = manifest.GetVideoOnlyStreams()
                .Where(s => s.Container == Container.Mp4)
                .OrderByDescending(s => s.VideoQuality)
                .ToList();

            options.Add(new StreamOption
            {
                Kind = MediaKind.Video,
                Label = "Highest available (best quality & size)",
                IsHighest = true
            });

            foreach (var s in streams.DistinctBy(s => s.VideoQuality.Label))
            {
                options.Add(new StreamOption
                {
                    Kind = MediaKind.Video,
                    TargetHeight = s.VideoQuality.MaxHeight,
                    Label = $"{s.VideoQuality.Label}  ·  MP4  ·  {FormatBytes(s.Size.Bytes)}"
                });
            }
        }
        else
        {
            var streams = PreferMp4Audio(manifest)
                .OrderByDescending(s => s.Bitrate)
                .ToList();

            options.Add(new StreamOption
            {
                Kind = MediaKind.Audio,
                Label = "Highest quality (best bitrate) → MP3",
                IsHighest = true
            });

            foreach (var s in streams)
            {
                options.Add(new StreamOption
                {
                    Kind = MediaKind.Audio,
                    TargetBitrate = s.Bitrate.BitsPerSecond,
                    Label = $"{Math.Round(s.Bitrate.KiloBitsPerSecond)} kbps  ·  {FormatBytes(s.Size.Bytes)} → MP3"
                });
            }
        }

        return options;
    }

    public async Task<string> DownloadAsync(
        VideoEntry video,
        MediaKind kind,
        StreamOption option,
        string outputDirectory,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var manifest = await _youtube.Videos.Streams.GetManifestAsync(video.Url, ct);
        var title = SanitizeText(video.Title);
        var outputFileBase = Path.Combine(outputDirectory, title);

        if (kind == MediaKind.Audio)
        {
            var audio = PickAudio(manifest, option);
            var container = audio.Container.Name;
            var downloadedPath = $"{outputFileBase}.{container}";

            // Reserve the last slice of the bar for the (progress-less) mp3 conversion.
            var scaled = progress is null ? null : new Progress<double>(p => progress.Report(p * 0.95));
            await _youtube.Videos.Streams.DownloadAsync(audio, downloadedPath, scaled, ct);

            var converter = new MP3Converter(_ffmpegDirectory);
            var finalPath = converter.Convert(outputFileBase, container)
                ? $"{outputFileBase}.mp3"
                : downloadedPath;

            progress?.Report(1.0);
            return finalPath;
        }
        else
        {
            var videoStream = PickVideo(manifest, option);
            var audioStream = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var finalPath = $"{outputFileBase}.mp4";
            var ffmpegExe = Path.Combine(_ffmpegDirectory, "ffmpeg.exe");

            var request = new ConversionRequestBuilder(finalPath)
                .SetFFmpegPath(ffmpegExe)
                .Build();

            await _youtube.Videos.DownloadAsync(
                new IStreamInfo[] { audioStream, videoStream }, request, progress, ct);

            progress?.Report(1.0);
            return finalPath;
        }
    }

    private static IEnumerable<AudioOnlyStreamInfo> PreferMp4Audio(StreamManifest manifest)
    {
        var all = manifest.GetAudioOnlyStreams().ToList();
        var mp4 = all.Where(s => s.Container == Container.Mp4).ToList();
        return mp4.Count > 0 ? mp4 : all;
    }

    private static VideoOnlyStreamInfo PickVideo(StreamManifest manifest, StreamOption option)
    {
        var streams = manifest.GetVideoOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .OrderByDescending(s => s.VideoQuality)
            .ToList();

        if (streams.Count == 0)
            streams = manifest.GetVideoOnlyStreams().OrderByDescending(s => s.VideoQuality).ToList();

        if (option.IsHighest)
            return streams.First();

        // Closest available height to the requested target.
        return streams
            .OrderBy(s => Math.Abs(s.VideoQuality.MaxHeight - option.TargetHeight))
            .ThenByDescending(s => s.VideoQuality)
            .First();
    }

    private static AudioOnlyStreamInfo PickAudio(StreamManifest manifest, StreamOption option)
    {
        var streams = PreferMp4Audio(manifest).OrderByDescending(s => s.Bitrate).ToList();

        if (option.IsHighest)
            return streams.First();

        return streams
            .OrderBy(s => Math.Abs(s.Bitrate.BitsPerSecond - option.TargetBitrate))
            .ThenByDescending(s => s.Bitrate)
            .First();
    }

    private static VideoEntry ToEntry(
        string url, string title, string author, TimeSpan? duration, IReadOnlyList<Thumbnail> thumbnails)
        => new()
        {
            Url = url,
            Title = title,
            Author = author,
            Duration = duration,
            ThumbnailUrl = thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url
        };

    private static string SanitizeText(string fileName)
        => string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.#} {units[unit]}";
    }
}
