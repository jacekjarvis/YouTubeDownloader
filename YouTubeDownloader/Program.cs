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
        Console.WriteLine("Enter your Youtube Link:");
        //string link = @"https://www.youtube.com/watch?v=17ZrLitIfRE";
        string link = Console.ReadLine().Trim();


        Console.WriteLine("\nGetting data...");

        //var videos = youtube.GetAllVideosAsync(link).GetAwaiter().GetResult().ToList();
        List<YouTubeVideo> videos = youtube.GetAllVideos(link).ToList();
        var audios = videos.Where(r => r.AdaptiveKind == AdaptiveKind.Audio).ToList();
        videos = videos.Where(vid => vid.Format == VideoFormat.Mp4 && vid.AudioBitrate > 0).ToList();


        //foreach (var b in audios)
        //{
        //    Console.WriteLine($"{b.AudioBitrate} {b.Format} {b.AudioFormat}");

        //}

        YouTubeVideo video;
        if (videos.Count == 0)
        {
            video = youtube.GetVideo(link);
            Console.WriteLine(video.FullName);
            Console.WriteLine($"{1}. Resolution {video.Resolution} , Audio Bitrate {video.AudioBitrate} {video.AudioFormat}");
            
        }
        else
        {
            Console.WriteLine(videos[0].FullName);
            int highestRes = 0;
            int highestResIndex = 0;
            for (int i = 0; i<videos.Count; i++)
            {
                Console.WriteLine($"{i+1}. Resolution {videos[i].Resolution} , Audio Bitrate {videos[i].AudioBitrate} {videos[i].AudioFormat}");
                if (videos[i].Resolution > highestRes)
                {
                    highestRes = videos[i].Resolution;
                    highestResIndex = i;
                }
            }
            int finalResOption = highestResIndex;
            Console.WriteLine();

            if (videos.Count != 1)
            {
                Console.WriteLine($"Select a Resolution Option (DEFAULT is Option: {highestResIndex+1}. Resolution {highestRes})");
                string selectedResOption = Console.ReadLine().Trim();


                if (selectedResOption != "" && selectedResOption != null)
                {
                    finalResOption = int.Parse(selectedResOption) - 1;
                }
            }
            video = videos[finalResOption];
        }



        //Thread.Sleep(450);
        string destination = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = $@"{destination}\{video.FullName}";
        DeleteFile(filePath);

        Console.WriteLine("Downloading...");
        File.WriteAllBytes(filePath, video.GetBytes());
        //File.WriteAllBytes(filePath, audios[0].GetBytes());

        Console.WriteLine("FINISHED: ");
        Console.WriteLine($"{filePath}");
        Console.ReadLine();
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


