using YouTubeDownloader.Core;

public class Program
{
    public static async Task Main()
    {
        var app = new YoutubeDownloaderApp(new YoutubeService());
        await app.RunAsync();
    }
}
