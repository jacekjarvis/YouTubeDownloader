using FFMpegCore;
using System;
using System.IO;

namespace YouTubeDownloader.Utility;

public class MP3Converter
{
    public MP3Converter(string ffmpegPath)
    {
        GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }

    public bool Convert(string source, string fileType)
    {
        try
        {
            Console.WriteLine($"Converting file to mp3...");
            FFMpeg.ExtractAudio($"{source}.{fileType}", $"{source}.mp3");
            File.Delete($"{source}.{fileType}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occured when converting file to mp3: {ex.Message}");
        }
        return false;
    }
}
