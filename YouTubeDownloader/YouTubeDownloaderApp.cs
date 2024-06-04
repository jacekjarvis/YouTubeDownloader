//using FFMpegCore; //Install-Package FFMpegCore //Install-Package Xabe.FFmpeg.Downloader
using VideoLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class YouTubeDownloaderApp
{
    private const string Version = "2024.06.04";
    private YouTube _youTube { get; }

    public YouTubeDownloaderApp()
    {
        _youTube = YouTube.Default;
    }

    public void Run()
    {
        PrintAppTitle();
        PromptUserForYouTubeLink();
        string link = ReadYouTubeLink();

        PromptUserForMediaType();
        string mediaType = ReadMediaType();

        GetMedia(link, mediaType);
    }

    private void GetMedia(string link, string mediaType)
    {
        Console.WriteLine(Environment.NewLine + "Getting data...");
        List<YouTubeVideo> videos = _youTube.GetAllVideos(link).ToList();

        if (mediaType.ToUpper() == "V")
        {
            videos = videos.Where(vid => vid.Format == VideoFormat.Mp4 && vid.AudioBitrate > 0)
                           .OrderByDescending(vid => vid.Resolution).ToList();
        }
        else
        {
            YouTubeVideo v = videos.Where(r => r.AdaptiveKind == AdaptiveKind.Audio).FirstOrDefault();
            videos.Clear();
            videos.Add(v);
        }

        YouTubeVideo video = SelectMediaOption(link, videos, mediaType);
        DownloadFile(mediaType, video);
    }

    private string ReadYouTubeLink()
    {
        //string link = @"https://www.youtube.com/watch?v=17ZrLitIfRE";  //For Testing
        return Console.ReadLine().Trim();
    }

    private void PromptUserForYouTubeLink()
    {
        Console.WriteLine("Enter your Youtube Link:");
    }

    private void PrintAppTitle()
    {
        Console.WriteLine($"GENN-SAMA'S YOUTUBE DOWNLOADER v{Version}");
        Console.WriteLine("------------------------------------------");
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
        fileName = GetValidFileName(fileName);

        string filePath = Path.Combine(destination, fileName);

        return filePath;
    }

    private  string GetValidFileName(string fileName)
    {
        //Ensure that the filename has Valid FileNameChars - so that we can save the file
        char[] invalidChars = Path.GetInvalidFileNameChars();

        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c.ToString(), "");
        }

        if (fileName.Length == 0)
        {
            DateTime now = DateTime.Now;
            return $"YouTube_{now.Year}-{now.Month}-{now.Day}";
        }
        return fileName;
    }

    public void PromptUserForMediaType()
    {
        Console.WriteLine("Select a format - Enter V or A");
        Console.WriteLine("[V]ideo (DEFAULT)");
        Console.WriteLine("[A]udio");
    }

    public string ReadMediaType()
    {
        string mediaType = Console.ReadLine().Trim();
        if (string.IsNullOrEmpty(mediaType))
        {
            return "V";
        }

        return mediaType;
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
        }
        else
        {
            PromptUserForTheResolution(videos);
            video = ReadTheResolution(videos);
        }

        return video;
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

    
}


