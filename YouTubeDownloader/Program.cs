//using FFMpegCore; //Install-Package FFMpegCore //Install-Package Xabe.FFmpeg.Downloader
using VideoLibrary;
using System.Web;
//using Xabe.FFmpeg.Downloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
//using Xabe.FFmpeg;

public class YouTubeDownloader
{
    public static YouTube youtube = YouTube.Default;

    public static void Main(string[] args)
    {
        Console.WriteLine("GENN-SAMA'S YOUTUBE DOWNLOADER");
        Console.WriteLine("------------------------------");

        Console.WriteLine("Enter your Youtube Link:");
        //string link = @"https://www.youtube.com/watch?v=17ZrLitIfRE";
        string link = Console.ReadLine().Trim();
        string mediaType = PromptUserForMediaType();

        Console.WriteLine("\nGetting data...");

        //var videos = youtube.GetAllVideosAsync(link).GetAwaiter().GetResult().ToList();
        List<YouTubeVideo> videos = youtube.GetAllVideos(link).ToList();

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

    private static void DownloadFile(string mediaType, YouTubeVideo video)
    {
        //Thread.Sleep(450);
        string filePath = GetFilePath(mediaType, video);
        DeleteFile(filePath);

        Console.WriteLine();
        Console.WriteLine("Downloading...");

        try
        {
            File.WriteAllBytes(filePath, video.GetBytes());
            //File.WriteAllBytes(filePath, audios[0].GetBytes());

            Console.WriteLine("FINISHED: ");
            Console.WriteLine($"{filePath}");

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occured while download/writing the file");
            Console.WriteLine(ex.ToString());
        }
        Console.ReadLine();
    }

    private static string GetFilePath(string mediaType, YouTubeVideo video)
    {
        string destination = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        string audioFileName = $"{video.FullName}.{video.AudioFormat}";
        string fileName = mediaType.ToUpper() == "V" ? video.FullName : audioFileName;
        fileName = ValidateFileName(fileName);

        string filePath = Path.Combine(destination, fileName);

        return filePath;
    }

    private static string ValidateFileName(string fileName)
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

    private static string PromptUserForMediaType()
    {
        Console.WriteLine("Select a format - Enter V or A");
        Console.WriteLine("[V]ideo (DEFAULT)");
        Console.WriteLine("[A]udio");

        string mediaType = Console.ReadLine().Trim();
        if (String.IsNullOrEmpty(mediaType))
        {
            return "V";
        }

        return mediaType;
    }

    private static YouTubeVideo SelectMediaOption(string link, List<YouTubeVideo> videos, string mediaType)
    {
        YouTubeVideo video = videos[0];
        Console.WriteLine(videos[0].Title);

        if (mediaType.ToUpper() == "A")
        {
            return video;
        }

        if (videos.Count == 1)
        {
            string message = $"[{1}] Resolution {video.Resolution} , Audio Bitrate {video.AudioBitrate} {video.AudioFormat}";
            Console.WriteLine(message);
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"Select a Resolution - Enter a number:");
            int i = 1;
            foreach(var vid in videos)
            {
                string defaultString = i == 1 ? "(DEFAULT)" : ""; 
                string message = $"[{i}] Resolution {vid.Resolution} , Audio Bitrate {vid.AudioBitrate} {vid.AudioFormat} {defaultString}";
                Console.WriteLine(message);
                i++;
            }

            string resolution = Console.ReadLine().Trim();
            if (!String.IsNullOrEmpty(resolution))
            {
                video = videos[int.Parse(resolution) - 1];
            }
        }

        return video;
    }

    public static void DeleteFile(string path)
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


