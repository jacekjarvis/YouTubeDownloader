using VideoLibrary;
using Xabe.FFmpeg;

public class YouTubeDownloader
{

    public static YouTube youtube = YouTube.Default;
    public static string selectedAudioQuality = "";
    public static string selectedVideoQuality = "";
    public static bool audioOnly = false; 

    public static void Main(string[] args)
    {
        Console.WriteLine("Enter your Youtube Link:");
        //string link = @"https://www.youtube.com/watch?v=BF0uf7apZDQ";
        string link =  Console.ReadLine().Trim();

        //Console.WriteLine("Processing");
        //GetVideoData(link);

        Console.WriteLine("Downloading");

        YouTubeVideo video = youtube.GetVideo(link);
        string destination = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath =  $@"{destination}\{video.FullName}";

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        if (!audioOnly) 
        {
            //download the full movie
            File.WriteAllBytes(filePath, video.GetBytes());
        }
        else
        {
            FFmpeg.ExtractAudio(audiomp4, txtFilePath + TargetVideo.ToList()[0].Title + "mp3");
        }

        Console.WriteLine("FINISHED - File Downloaded to: ");
        Console.WriteLine($"{destination}");
        Console.ReadLine();
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