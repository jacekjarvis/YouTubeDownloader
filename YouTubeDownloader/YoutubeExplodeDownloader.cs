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
    public IEnumerable<string> GetVideoOptions();
    public IEnumerable<string> GetAudioOptions();

    public Task DownloadVideoOption(int option, string filePath);

    public void DownloadAudioOption(int option, string outputFilePath);
    string GetVideoTitle();
}



public class YoutubeExplodeDownloader : IYoutubeDownloader
{
    private readonly YoutubeClient _youtube;
    private List<MuxedStreamInfo> _muxedStreams;
    public string VideoUrl { get; set; }

    public YoutubeExplodeDownloader()
    {
        _youtube = new YoutubeClient();
    }


    public string GetVideoTitle()
    {
        var video = _youtube.Videos.GetAsync(VideoUrl).Result;
        return video.Title;
    }

    private string GetSanitizedVideoTitle()
    {
        var video = _youtube.Videos.GetAsync(VideoUrl).Result;
        return SanitizeTitle(video.Title);
    }

    public IEnumerable<string> GetVideoOptions()
    {
        var streamManifest = _youtube.Videos.Streams.GetManifestAsync(VideoUrl).Result;
        _muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(stream => stream.VideoQuality).ToList();

        return _muxedStreams.Select(stream => $"Video Quality: {stream.VideoQuality} | " +
                                              $"Video Resolution: {stream.VideoResolution} | " + 
                                              $"File Type: {stream.Container}");
    }

    public IEnumerable<string> GetAudioOptions()
    {
        throw new NotImplementedException();
    }

    public async Task DownloadVideoOption(int option, string outputPath)
    {
        var streamInfo = _muxedStreams[option - 1];

        var title = GetSanitizedVideoTitle();

        var outputFilePath = Path.Combine(outputPath, $"{title}.{streamInfo.Container}");
        await _youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);


        var datetime = DateTime.Now;
        Console.WriteLine("Download completed!");
        Console.WriteLine($"Video saved as: {outputFilePath} {datetime}");

    }

    public void DownloadAudioOption(int option, string outputFilePath)
    {
        throw new NotImplementedException();
    }


    private string SanitizeTitle(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }


}