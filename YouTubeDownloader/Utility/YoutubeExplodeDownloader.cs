using System;
using System.Collections.Generic;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Linq;
using System.Threading.Tasks;
using System.IO;


public class YoutubeExplodeDownloader : IYoutubeDownloader
{
    public string VideoUrl { get; set; }
    private string _videoTitle { get; set; }

    private readonly YoutubeClient _youtube;
    private List<IStreamInfo> _streams;

    public YoutubeExplodeDownloader()
    {
        _youtube = new YoutubeClient();
    }

    public string GetVideoTitle()
    {
        var video = _youtube.Videos.GetAsync(VideoUrl).Result;
        _videoTitle = video.Title;
        return video.Title;
    }

    public IEnumerable<string> GetVideoOptions()
    {
        var streamManifest = _youtube.Videos.Streams.GetManifestAsync(VideoUrl).Result;
        _streams = streamManifest.GetMuxedStreams()
            .OrderByDescending(stream => stream.VideoQuality)
            .Select(stream => (IStreamInfo)stream).ToList();

        var result = _streams.Select(stream =>
        {
            var muxedStream = (MuxedStreamInfo)stream;
            return $"File Type: {muxedStream.Container} | " +
                   $"Video Quality: {muxedStream.VideoQuality} | " +
                   $"Video Resolution: {muxedStream.VideoResolution} | " +
                   $"Size: {muxedStream.Size} ";
        });
        return result;
    }

    public IEnumerable<string> GetAudioOptions()
    {
        var streamManifest = _youtube.Videos.Streams.GetManifestAsync(VideoUrl).Result;
        _streams = streamManifest.GetAudioOnlyStreams()
            .OrderByDescending(stream => stream.AudioCodec)
            .ThenBy(stream => stream.Bitrate)
            .Select(stream => (IStreamInfo)stream).ToList();

        var result = _streams.Select(stream =>
        {
            var audioStream = (AudioOnlyStreamInfo)stream;
            return $"File Type: {audioStream.Container} | " +
                   $"Audio Container: {audioStream.AudioCodec} | " +
                   $"Bitrate: {audioStream.Bitrate} | " +
                   $"Size: {audioStream.Size} ";
        });
        return result;
    }

    public async Task DownloadMedia(int option, string outputPath)
    {
        var streamInfo = _streams[option];

        var title = _videoTitle;
        if (string.IsNullOrEmpty(title))
        {
            title = GetVideoTitle();
        }
        title = SanitizeText(title);

        var outputFilePath = Path.Combine(outputPath, $"{title}.{streamInfo.Container}");
        await _youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);

        var datetime = DateTime.Now;
        Console.WriteLine($"Download completed: {datetime}");
        Console.WriteLine($"Video saved as: {outputFilePath}");
    }

    private static string SanitizeText(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }
}