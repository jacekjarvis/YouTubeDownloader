//using FFMpegCore; //Install-Package FFMpegCore //Install-Package Xabe.FFmpeg.Downloader

using YoutubeExplode;
using VideoLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Videos;
using System.Net.Http;
using System.Threading.Tasks;

public class YoutubeDownloaderApp
{
    private const string Version = "2024.06.04";
    private readonly string OutputPath;
    private YouTube _youTube { get; }
    private YoutubeClient youtube {  get; }

    private IYoutubeDownloader _youtubeDownloader { get; }

    public YoutubeDownloaderApp(IYoutubeDownloader youtubeDownloader)
    {
        _youTube = YouTube.Default;
        youtube = new YoutubeClient();
        _youtubeDownloader = youtubeDownloader;
        OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public void Run()
    {
        PrintAppTitle();
        PromptUserForYouTubeLink();
        var videoUrl = EnterYouTubeUrl();
        _youtubeDownloader.VideoUrl = videoUrl;

        var title = _youtubeDownloader.GetVideoTitle();
        Console.WriteLine(title);

        PromptUserForMediaType();
        var mediaType = SelectMediaType();

        DisplayOptions(mediaType);
        var option = 1;  //SelectOption();

        DownloadSelectedOption(option);

        //GetMediaAsync(videoUrl, mediaType).Wait();
    }



    private async Task GetMediaAsync(string videoUrl, string mediaType)
    {


        var video = youtube.Videos.GetAsync(videoUrl).Result;
        var streamManifest = youtube.Videos.Streams.GetManifestAsync(videoUrl).Result;

        //var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

        var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

        var sanitizedTitle = SanitizeTitle(video.Title);

        //var stream = youtube.Videos.Streams.GetAsync(streamInfo).Result;



        if (muxedStreams.Any())
        {
            var streamInfo = muxedStreams.First();
            //using var httpClient = new HttpClient();
            //var stream = httpClient.GetStreamAsync(streamInfo.Url).Result;

            var outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        $"{sanitizedTitle}.{streamInfo.Container}");

            await youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);
            //using var outputStream = File.Create(outputFilePath);
            //await stream.CopyToAsync(outputStream);

            var datetime = DateTime.Now;
            Console.WriteLine("Download completed!");
            Console.WriteLine($"Video saved as: {outputFilePath} {datetime}");
        }
        else
        {
            Console.WriteLine($"No suitable video stream found for {video.Title}.");
        }


        //youtube.Videos.Streams.DownloadAsync(streamInfo, $"video.{streamInfo.Container}");
        //await youtube.Videos.Streams.DownloadAsync(streamInfo, path);

        //if (mediaType.ToUpper() == "V")
        //{
        //    videos = videos.Where(vid => vid.Format == VideoFormat.Mp4 && vid.AudioBitrate > 0)
        //                   .OrderByDescending(vid => vid.Resolution).ToList();
        //}
        //else
        //{
        //    YouTubeVideo v = videos.Where(r => r.AdaptiveKind == AdaptiveKind.Audio).FirstOrDefault();
        //    videos.Clear();
        //    videos.Add(v);
        //}

        //YouTubeVideo video = SelectMediaOption(videoUrl, videos, mediaType);
        //DownloadFile(mediaType, video);
        Console.WriteLine("Get Media End");
    }

    private async Task GetMediaAsync1(string videoUrl, string mediaType)
    {
        
        //List<YouTubeVideo> videos = _youTube.GetAllVideos(videoUrl).ToList();

        var video = youtube.Videos.GetAsync(videoUrl).Result;
        var streamManifest = youtube.Videos.Streams.GetManifestAsync(videoUrl).Result;

        //var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

        var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

        var sanitizedTitle = SanitizeTitle(video.Title);

        //var stream = youtube.Videos.Streams.GetAsync(streamInfo).Result;

        

        if (muxedStreams.Any())
        {
            var streamInfo = muxedStreams.First();
            //using var httpClient = new HttpClient();
            //var stream = httpClient.GetStreamAsync(streamInfo.Url).Result;
            
            var outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        $"{sanitizedTitle}.{streamInfo.Container}");

            await youtube.Videos.Streams.DownloadAsync(streamInfo, outputFilePath);
            //using var outputStream = File.Create(outputFilePath);
            //await stream.CopyToAsync(outputStream);

            var datetime = DateTime.Now;
            Console.WriteLine("Download completed!");
            Console.WriteLine($"Video saved as: {outputFilePath} {datetime}");
        }
        else
        {
            Console.WriteLine($"No suitable video stream found for {video.Title}.");
        }
    

    //youtube.Videos.Streams.DownloadAsync(streamInfo, $"video.{streamInfo.Container}");
    //await youtube.Videos.Streams.DownloadAsync(streamInfo, path);

    //if (mediaType.ToUpper() == "V")
    //{
    //    videos = videos.Where(vid => vid.Format == VideoFormat.Mp4 && vid.AudioBitrate > 0)
    //                   .OrderByDescending(vid => vid.Resolution).ToList();
    //}
    //else
    //{
    //    YouTubeVideo v = videos.Where(r => r.AdaptiveKind == AdaptiveKind.Audio).FirstOrDefault();
    //    videos.Clear();
    //    videos.Add(v);
    //}

    //YouTubeVideo video = SelectMediaOption(videoUrl, videos, mediaType);
    //DownloadFile(mediaType, video);
    Console.WriteLine("Get Media End");
    }

    private string EnterYouTubeUrl()
    {
        return @"https://www.youtube.com/watch?v=17ZrLitIfRE";  //For Testing
        //return Console.ReadLine().Trim();
    }

    private void PromptUserForYouTubeLink()
    {
        Console.WriteLine("Enter your Youtube Link:");
    }

    private void PrintAppTitle()
    {
        Console.WriteLine($"JARVO'S YOUTUBE DOWNLOADER v{Version}");
        Console.WriteLine("--------------------------------------");
    }

    public void DownloadFile(string mediaType, YouTubeVideo video)
    {
        string filePath = GetFilePath(mediaType, video);
        DeleteFile(filePath);

        Console.WriteLine(Environment.NewLine + "Downloading...");

        try
        {
            File.WriteAllBytes(filePath, video.GetBytes());

            Console.WriteLine("FINISHED: ");
            Console.WriteLine($"{filePath}");

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occured while download/writing the file");
            Console.WriteLine(ex);
        }
        Exit();
    }

    private static void Exit()
    {
        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }

    private string GetFilePath(string mediaType, YouTubeVideo video)
    {
        string destination = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        string audioFileName = $"{video.FullName}.{video.AudioFormat}";
        string fileName = mediaType.ToUpper() == "V" ? video.FullName : audioFileName;
        fileName = SanitizeTitle(fileName);

        string filePath = Path.Combine(destination, fileName);

        return filePath;
    }

    private  string SanitizeTitle(string fileName)
    {
        //Ensure that the filename has Valid FileNameChars - so that we can save the file
        //char[] invalidChars = Path.GetInvalidFileNameChars();

        //foreach (char c in invalidChars)
        //{
        //    fileName = fileName.Replace(c.ToString(), "");
        //}

        //if (fileName.Length == 0)
        //{
        //    DateTime now = DateTime.Now;
        //    return $"YouTube_{now.Year}-{now.Month}-{now.Day}";
        //}
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    public void PromptUserForMediaType()
    {
        Console.WriteLine("Select a format - Enter V or A");
        Console.WriteLine("[V]ideo (DEFAULT)");
        Console.WriteLine("[A]udio");
    }

    public string SelectMediaType()
    {
        return "V";
        //string mediaType = Console.ReadLine().Trim();
        //if (string.IsNullOrEmpty(mediaType))
        //{
        //    return "V";
        //}

        //return mediaType;
    }

    public YouTubeVideo SelectMediaOption(string link, List<YouTubeVideo> videos, string mediaType)
    {
        YouTubeVideo video = videos[0];
        Console.WriteLine(video.Title);

        if (mediaType.ToUpper() == "A")
        {
            return video;
        }

        if (videos.Count == 1)
        {
            string message = $"[{1}] Resolution {video.Resolution} , " + 
                             $"Audio Bitrate {video.AudioBitrate} {video.AudioFormat}" +
                             Environment.NewLine ;
            Console.WriteLine(message);
            return video;
        }

        PromptUserForTheResolution(videos);
        var selectedVideo = ReadTheResolution(videos);

        if (selectedVideo != null)
        {
            return selectedVideo;
        }

        return video;
    }

    private void PromptUserForTheResolution(List<YouTubeVideo> videos)
    {
        Console.WriteLine($"Select a Resolution - Enter a number:");
        int i = 1;
        foreach (var vid in videos)
        {
            string defaultstring = i == 1 ? "(DEFAULT)" : "";
            string message = $"[{i}] Resolution {vid.Resolution} , Audio Bitrate {vid.AudioBitrate} {vid.AudioFormat} {defaultstring}";
            Console.WriteLine(message);
            i++;
        }
    }

    private YouTubeVideo ReadTheResolution(List<YouTubeVideo> videos)
    {
        YouTubeVideo video = default;
        string resolution = Console.ReadLine().Trim();
        if (!string.IsNullOrEmpty(resolution))
        {
            video = videos[int.Parse(resolution) - 1];
        }

        return video;
    }

    public void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch 
        { 
            Console.WriteLine("Error Deleting the existing File"); 
        }
    }

    private void DownloadSelectedOption(int option)
    {
        _youtubeDownloader.DownloadMedia(option, OutputPath).Wait();

    }

    private object SelectOption()
    {
        throw new NotImplementedException();
    }

    private void DisplayOptions(string mediaType)
    {
        Console.WriteLine("Getting data...");
        if (mediaType == "V")
        {
            var options = _youtubeDownloader.GetVideoOptions();
            var i = 1;
            foreach (var option in options)
            {
                Console.WriteLine($"[{i}] {option}");
                i++;
            }
        }
    }

}

