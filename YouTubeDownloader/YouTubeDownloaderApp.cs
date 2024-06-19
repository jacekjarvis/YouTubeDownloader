using YoutubeExplode;
using VideoLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

public class YoutubeDownloaderApp
{
    private const string Version = "2024.06.19";
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
        DisplayApplicationTitle();
        PromptForUrl();
        var videoUrl = GetUrl();
        GetAndDisplayTitle(videoUrl);

        PromptForMediaType();
        var mediaType = GetMediaType();

        var options = GetMediaOptions(mediaType);
        DisplayOptions(options);

        int option = 1;
        if (options.Count > 1)
        {
            option = GetOption(options);
        }

        DownloadSelectedOption(option);
        Exit();
    }

    private void GetAndDisplayTitle(string videoUrl)
    {
        _youtubeDownloader.VideoUrl = videoUrl;

        Console.WriteLine("Getting Data...");
        var videoTitle = _youtubeDownloader.GetVideoTitle();
        Console.WriteLine(videoTitle);
    }

    private List<string> GetMediaOptions(string mediaType)
    {
        Console.WriteLine("Getting Data...");
        if (mediaType == "A") 
        {
            return _youtubeDownloader.GetAudioOptions().ToList();
        }

        return _youtubeDownloader.GetVideoOptions().ToList();
    }

    private string GetUrl()
    {
        //return @"https://www.youtube.com/watch?v=17ZrLitIfRE";  //For Testing
        return Console.ReadLine().Trim();
    }

    private void PromptForUrl()
    {
        Console.WriteLine("Enter your Youtube Link:");
    }

    private void DisplayApplicationTitle()
    {
        Console.WriteLine($"JARVO'S YOUTUBE DOWNLOADER v{Version}");
        Console.WriteLine("--------------------------------------");
    }



    private static void Exit()
    {
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
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

    private static string SanitizeTitle(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    public static void PromptForMediaType()
    {
        Console.WriteLine("Select a format - Enter V or A");
        Console.WriteLine("[V]ideo (DEFAULT)");
        Console.WriteLine("[A]udio");
    }



    private void DownloadSelectedOption(int option)
    {
        Console.WriteLine($"Downloading... {option}");
        _youtubeDownloader.DownloadMedia(option-1, OutputPath).Wait();
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
        int result;
        while (true)
        {
            Console.Write($"Please enter an integer (1 to {options.Count}): ");
            string input = Console.ReadLine();
            if (int.TryParse(input, out result) && result >= 1 && result <= options.Count)
            {
                break;
            }
            else
            {
                Console.WriteLine("Invalid input. Returning default option 1.");
                return 1;
            }
        }
        return result;
    }

    public static string GetMediaType()
    {
        char mediaType = Console.ReadKey().KeyChar;
        Console.WriteLine();
        mediaType = char.ToUpper(mediaType);

        if (mediaType == 'V' || mediaType == 'A')
        {
            return mediaType.ToString();
        }
        else
        {
            return "V";
        }
    }
}

