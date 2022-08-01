using FFMpegCore; //Install-Package FFMpegCore //Install-Package Xabe.FFmpeg.Downloader
using VideoLibrary;
using System.Web;
using Xabe.FFmpeg.Downloader;
//using Xabe.FFmpeg;

public class YouTubeDownloader
{

    public static YouTube youtube = YouTube.Default;
    public static string selectedAudioQuality = "";
    public static string selectedVideoQuality = "";
    public static bool audioOnly = false; 

    public static void Main(string[] args)
    {
        Console.WriteLine("Enter your Youtube Link:");
        //string link = @"https://www.youtube.com/watch?v=OBF4kZS9baw";
        string link =  Console.ReadLine().Trim();

        //Console.WriteLine("Processing");
        //GetVideoData(link);

        Console.WriteLine("Audio ONLY? Y or N:");
        string selectedAudioOnly = Console.ReadLine().Trim().ToUpper();

        if (selectedAudioOnly == "Y")
        {
            audioOnly = true;
        }

        Console.WriteLine("Downloading");

        YouTubeVideo video = youtube.GetVideo(link);
        string destination = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath =  $@"{destination}\{video.FullName}";


        DeleteFile(filePath);

        File.WriteAllBytes(filePath, video.GetBytes());

        if (audioOnly) 
        {
            string tempFileName = filePath;
            filePath = $@"{destination}\{video.Title}.mp3";

            DeleteFile(filePath);

            //Console.WriteLine(tempFileName);
            //Console.WriteLine(filePath);

            if (!File.Exists("ffmpeg.exe"))
                FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);

            ConvertAudio(tempFileName, filePath);
            DeleteFile(tempFileName);

        }

        Console.WriteLine("FINISHED - File Downloaded to: ");
        Console.WriteLine($"{destination}");
        Console.ReadLine();
    }

    public static async Task ConvertAudio(string tempFileName, string filePath)
    {

        //var snippet = FFmpeg.Conversions.FromSnippet.Convert(tempFileName, filePath);
        //var snippet = FFmpeg.Conversions.FromSnippet.ExtractAudio(tempFileName, filePath);
        //snippet.Start();
        Console.WriteLine("Converting to mp3...");
        FFMpeg.ExtractAudio(tempFileName, filePath);
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


        /////////////////////////////////////////////////////////////////////////////////////////////
        Console.WriteLine("Select reslolution: ");

        int i = 1;
        foreach (var item in resolution)
        {

            Console.WriteLine($"{i}) {item}");
            i++;

        }

        string selected = Console.ReadLine().Trim();
        try
        {
            int index = int.Parse(selected);
            selectedVideoQuality = resolution[index-1].ToString();

        }
        catch (Exception)
        {
            selectedVideoQuality = resolution[resolution.Count-1].ToString();
        }
        //Console.WriteLine(selectedVideoQuality);



        /////////////////////////////////////////////////////////////////////////////////////////////
        Console.WriteLine("Select Audio Quality (Bit rate): ");

        i = 1;
        foreach (var item in bitRate)
        {

            Console.WriteLine($"{i}) {item}");
            i++;

        }

        selected = Console.ReadLine().Trim();
        try
        {
            int index = int.Parse(selected);
            selectedAudioQuality = bitRate[index-1].ToString();

        }
        catch (Exception)
        {
            selectedAudioQuality = bitRate[bitRate.Count-1].ToString();
        }
        //Console.WriteLine(selectedAudioQuality);


    }
}




//using VideoLibrary;

//   void SaveVideoToDisk(string link)
//{
//    var youTube = YouTube.Default;
//    var video = youTube.GetVideo(link);
//    File.WriteAllBytes(@"C:\" + video.FullName, video.GetBytes());
//}