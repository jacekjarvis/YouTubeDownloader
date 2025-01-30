using YoutubeExplode;
using System;
using System.Collections.Generic;
using System.Linq;


public class YoutubeDownloaderApp
{
    private readonly string Version = "2025.01.29";
    private readonly string OutputPath;
    private IYoutubeDownloader _youtubeDownloader { get; }

    public YoutubeDownloaderApp(IYoutubeDownloader youtubeDownloader)
    {
        _youtubeDownloader = youtubeDownloader;
        OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public void Run()
    {
        DisplayApplicationTitle();
       
        PromptForUrl();
        var videoUrl = GetUrl();
        GetAndDisplayTitle(videoUrl);

        PromptForMediaType();
        var mediaType = GetMediaType();
        var options = GetMediaOptions(mediaType);

        DisplayOptions(options);
        var option = GetOption(options); 
        DownloadSelectedOption(option, mediaType);

        Exit();
    }
    private void DisplayApplicationTitle()
    {
        Console.WriteLine($"JARVO'S YOUTUBE DOWNLOADER v{Version}");
        Console.WriteLine("--------------------------------------");
    }
    private void PromptForUrl()
    {
        Console.WriteLine("Enter your Youtube Link:");
    }
    private string GetUrl()
    {
        //return @"https://www.youtube.com/watch?v=17ZrLitIfRE";  //For Testing
        return Console.ReadLine().Trim();
    }

    private void GetAndDisplayTitle(string videoUrl)
    {
        _youtubeDownloader.VideoUrl = videoUrl;

        Console.WriteLine("Getting Data...");
        var videoTitle = _youtubeDownloader.GetVideoTitle();
        Console.WriteLine(videoTitle);
    }

    private static void PromptForMediaType()
    {
        Console.WriteLine("Select a format - Enter V or A");
        Console.WriteLine("[V]ideo (DEFAULT)");
        Console.WriteLine("[A]udio");
    }

    private static char GetMediaType()
    {
        char mediaType = Console.ReadKey().KeyChar;
        Console.WriteLine();
        mediaType = char.ToUpper(mediaType);

        if (mediaType == 'V' || mediaType == 'A')
        {
            return mediaType;
        }
        else
        {
            return 'V';
        }
    }

    private List<string> GetMediaOptions(char mediaType)
    {
        Console.WriteLine("Getting Data...");
        if (mediaType == 'A') 
        {
            return _youtubeDownloader.GetAudioOptions().ToList();
        }

        return _youtubeDownloader.GetVideoOptions().ToList();
    }

    private static void DisplayOptions(List<string> options)
    {
        var i = 1;
        foreach (var option in options)
        {
            Console.WriteLine($"[{i}] {option}");
            i++;
        }
    }

    private static int GetOption(List<string> options)
    {
        if (options.Count == 1)
        {
            return 1;
        }

        int selectedOption;
        while (true)
        {
            Console.Write($"Please enter an integer (1 to {options.Count}): ");
            string input = Console.ReadLine();
            if (int.TryParse(input, out selectedOption) && selectedOption >= 1 && selectedOption <= options.Count)
            {
                break;
            }
            else
            {
                return 1;
            }
        }
        return selectedOption;
    }

    private void DownloadSelectedOption(int option, char mediaType)
    {
        Console.WriteLine($"Downloading option: [{option}] ...");
        _youtubeDownloader.DownloadMedia((option-1), OutputPath).Wait();
    }

    private static void Exit()
    {
        Console.WriteLine("\nPress any key to exit");
        Console.ReadKey();
    }
}