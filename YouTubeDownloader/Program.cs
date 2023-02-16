//using FFMpegCore; //Install-Package FFMpegCore //Install-Package Xabe.FFmpeg.Downloader
using VideoLibrary;
using System.Web;
//using Xabe.FFmpeg.Downloader;
using System;
using System.Security.Cryptography;
using System.Runtime.ConstrainedExecution;
//using Xabe.FFmpeg;

public class YouTubeDownloader
{
    public static YouTube youtube = YouTube.Default;


    public static void Main(string[] args)
    {
        Console.WriteLine("JARVO'S YOUTUBE DOWNLOADER");
        Console.WriteLine("--------------------------");
        Console.WriteLine("Enter your Youtube Link:");
        //string link = @"https://www.youtube.com/watch?v=17ZrLitIfRE";
        string link = Console.ReadLine().Trim();

        Console.WriteLine("Video or Audio Only? (Enter V or A)");
        Console.WriteLine("(DEFAULT is Video)");

        string mediaType = Console.ReadLine().Trim();


        Console.WriteLine("\nGetting data...");

        //var videos = youtube.GetAllVideosAsync(link).GetAwaiter().GetResult().ToList();
        List<YouTubeVideo> videos = youtube.GetAllVideos(link).ToList();

        if (mediaType.ToUpper() == "V")
        {
            videos = videos.Where(vid => vid.Format == VideoFormat.Mp4 && vid.AudioBitrate > 0).ToList(); 
        }
        else
        {
            videos = videos.Where(r => r.AdaptiveKind == AdaptiveKind.Audio).ToList();
        }
        
        
        


        //foreach (var b in audios)
        //{
        //    Console.WriteLine($"{b.AudioBitrate} {b.Format} {b.AudioFormat}");

        //}
        
        YouTubeVideo video = SelectMediaOption(link, videos, mediaType);



        //Thread.Sleep(450);
        string destination = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        string audioFileName = $"{video.FullName}.{video.AudioFormat}";
        string fileName = mediaType.ToUpper() == "V" ? video.FullName : audioFileName;
        string filePath = $@"{destination}\{fileName}";
        DeleteFile(filePath);

        Console.WriteLine();
        Console.WriteLine("Downloading...");
        File.WriteAllBytes(filePath, video.GetBytes());
        //File.WriteAllBytes(filePath, audios[0].GetBytes());

        Console.WriteLine("FINISHED: ");
        Console.WriteLine($"{filePath}");
        Console.ReadLine();
    }

    private static YouTubeVideo SelectMediaOption(string link, List<YouTubeVideo> videos, string mediaType)
    {
        YouTubeVideo video = videos[0];
        Console.WriteLine(videos[0].Title);
        if (videos.Count == 0)
        {
            string message = $"{1}. Resolution {video.Resolution} , Audio Bitrate {video.AudioBitrate} {video.AudioFormat}";

            if (mediaType.ToUpper() == "A")
            {
                message = $"{video.AudioBitrate} {video.AudioFormat}";
            }
            Console.WriteLine(message);
            Console.WriteLine();

        }
        else
        {
            
            int highestQuality = 0;
            int highestQualityIndex = 0;
            for (int i = 0; i<videos.Count; i++)
            {
                string message = $"{i+1}. Resolution {videos[i].Resolution} , Audio Bitrate {videos[i].AudioBitrate} {videos[i].AudioFormat}";
                var quality = videos[i].Resolution;

                if (mediaType.ToUpper() == "A")
                {
                    message = $"{i+1}. {videos[i].AudioBitrate} {videos[i].AudioFormat}";
                    quality = videos[i].AudioBitrate;
                }
                Console.WriteLine(message);

                if (quality > highestQuality)
                {
                    highestQuality = mediaType.ToUpper() == "V" ? videos[i].Resolution : videos[i].AudioBitrate;
                    highestQualityIndex = i;
                }
            }
            int finalResOption = highestQualityIndex;
            Console.WriteLine();

            if (videos.Count != 1)
            {
                string type = mediaType.ToUpper() == "V" ? "Resolution" : "AudioBitrate";

                Console.WriteLine($"Select a Resolution Option (DEFAULT is Option: {highestQualityIndex+1}. {type} {highestQuality}");
                string selectedResOption = Console.ReadLine().Trim();

                if (selectedResOption != "" && selectedResOption != null)
                {
                    finalResOption = int.Parse(selectedResOption) - 1;
                }
            }
            video = videos[finalResOption];
        }

        return video;
    }

    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void GetVideoData(string link, bool playlist = false)
    {
        var videoData = youtube.GetAllVideos(link);
        var resolution = videoData.Where(r => r.AdaptiveKind == AdaptiveKind.Video && r.Format== VideoFormat.Mp4).
                         Select(r => r.Resolution).Distinct().ToList();
        var bitRate = videoData.Where(r => r.AdaptiveKind == AdaptiveKind.Audio).Select(r => r.AudioBitrate).Distinct().ToList();

        resolution.Sort();
        bitRate.Sort();

        Console.WriteLine("Select reslolution: ");

        int i = 1;
        foreach (var item in resolution)
        {
            Console.WriteLine($"{i}) {item}");
            i++;

        }

        Console.WriteLine("Select Audio Quality (Bit rate): ");

        i = 1;
        foreach (var item in bitRate)
        {

            Console.WriteLine($"{i}) {item}");
            i++;
        }

    }
}


