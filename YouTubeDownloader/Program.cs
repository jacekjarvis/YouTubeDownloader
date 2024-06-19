//using FFMpegCore; //Install-Package FFMpegCore //Install-Package Xabe.FFmpeg.Downloader
//using Xabe.FFmpeg;
//using Xabe.FFmpeg.Downloader;

public class Program
{
    public static void Main(string[] args)
    {
        var youtubeDownloader = new YoutubeExplodeDownloader();
        var downloader = new YoutubeDownloaderApp(youtubeDownloader);
        downloader.Run(); 
    }
}