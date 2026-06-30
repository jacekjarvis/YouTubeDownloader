using YouTubeDownloader.Core;

public class YoutubeDownloaderApp
{
    private const string Version = "2026.06.30";
    private readonly IYoutubeService _service;
    private readonly string _outputPath;

    public YoutubeDownloaderApp(IYoutubeService service)
    {
        _service = service;
        _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"JARVO'S YOUTUBE DOWNLOADER v{Version}");
        Console.WriteLine("--------------------------------------");

        Console.WriteLine("Enter your YouTube link (video or playlist):");
        var url = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.WriteLine("Getting data...");
        ResolvedUrl resolved;
        try
        {
            resolved = await _service.ResolveAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Exit();
            return;
        }

        if (resolved.IsPlaylist)
            Console.WriteLine($"Playlist: {resolved.Title}  ({resolved.Videos.Count} videos)");
        else
            Console.WriteLine(resolved.Title);

        var kind = PromptForMediaType();

        Console.WriteLine("Getting quality options...");
        var options = await _service.GetStreamOptionsAsync(resolved.Videos[0].Url, kind);
        var option = PromptForOption(options);

        var index = 1;
        foreach (var video in resolved.Videos)
        {
            Console.WriteLine($"\n[{index}/{resolved.Videos.Count}] {video.Title}");
            var progress = new Progress<double>(RenderProgressBar);
            try
            {
                var path = await _service.DownloadAsync(video, kind, option, _outputPath, progress);
                Console.WriteLine($"\nSaved: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFailed: {ex.Message}");
            }
            index++;
        }

        Console.WriteLine($"\nDownload completed: {DateTime.Now}");
        Exit();
    }

    private static MediaKind PromptForMediaType()
    {
        Console.WriteLine("Select a format - Enter V or A");
        Console.WriteLine("[V]ideo (DEFAULT)");
        Console.WriteLine("[A]udio");

        var key = char.ToUpper(Console.ReadKey().KeyChar);
        Console.WriteLine();
        return key == 'A' ? MediaKind.Audio : MediaKind.Video;
    }

    private static StreamOption PromptForOption(IReadOnlyList<StreamOption> options)
    {
        for (var i = 0; i < options.Count; i++)
            Console.WriteLine($"[{i + 1}] {options[i].Label}");

        if (options.Count == 1)
            return options[0];

        Console.Write($"Please enter an integer (1 to {options.Count}): ");
        var input = Console.ReadLine();
        if (int.TryParse(input, out var selected) && selected >= 1 && selected <= options.Count)
            return options[selected - 1];

        return options[0];
    }

    private static void RenderProgressBar(double fraction)
    {
        const int width = 30;
        var filled = (int)(fraction * width);
        var bar = new string('#', filled) + new string('-', width - filled);
        Console.Write($"\r[{bar}] {fraction:P0}");
    }

    private static void Exit()
    {
        Console.WriteLine("\nPress any key to exit");
        Console.ReadKey();
    }
}
