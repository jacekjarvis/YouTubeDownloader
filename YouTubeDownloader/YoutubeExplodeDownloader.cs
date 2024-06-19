using System;
using System.Collections.Generic;
using VideoLibrary;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Linq;
using System.Threading.Tasks;
using System.IO;


public interface IYoutubeDownloader
{
    public string VideoUrl { get; set; }
    public string CurrentTitle { get; set; }
    public IEnumerable<string> GetVideoOptions();
    public IEnumerable<string> GetAudioOptions();

    public Task DownloadMedia(int option, string outputPath);

    public string GetVideoTitle();
}



public class YoutubeExplodeDownloader : IYoutubeDownloader
{
    private readonly YoutubeClient _youtube;
    private List<IStreamInfo> _streams;
    public string VideoUrl { get; set; }
    public string CurrentTitle { get; set; }

    public YoutubeExplodeDownloader()
    {
        _youtube = new YoutubeClient();
    }


    public string GetVideoTitle()
    {
        var video = _youtube.Videos.GetAsync(VideoUrl).Result;
        CurrentTitle = video.Title;
        return video.Title;
    }


    public IEnumerable<string> GetVideoOptions()
    {
        var streamManifest = _youtube.Videos.Streams.GetManifestAsync(VideoUrl).Result;
        _streams = streamManifest.GetMuxedStreams().OrderByDescending(stream => stream.VideoQuality).Select(stream => (IStreamInfo)stream).ToList();

        var result = _streams.Select(stream =>
        {
            var muxedStream = (MuxedStreamInfo)stream;
            return  $"Video Quality: {muxedStream.VideoQuality} | " +
                    $"Video Resolution: {muxedStream.VideoResolution} | " +
                    $"File Type: {muxedStream.Container}";
        });
        return result;
    }


    public IEnumerable<string> GetAudioOptions()
    {
        throw new NotImplementedException();
    }

    public async Task DownloadMedia(int option, string outputPath)
    {
        var streamInfo = _streams[option - 1];

        var title = SanitizeText(CurrentTitle);

        var outputFilePath = Path.Combine(outputPath, $"{title}.{streamInfo.Container}");
        await _youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);


        var datetime = DateTime.Now;
        Console.WriteLine("Download completed!");
        Console.WriteLine($"Video saved as: {outputFilePath} {datetime}");

    }

    private string SanitizeText(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }


}