using FFMpegCore;
using System;
using System.IO;

namespace YouTubeDownloader.Utility; 

public class MP3Converter
{
    public bool Convert(string source, string fileType)
    {
        try
        {
            FFMpeg.ExtractAudio($"{source}.{fileType}", $"{source}.mp3");
            File.Delete($"{source}.{fileType}");
            return true;
        }
        catch
        {
            Console.WriteLine($"Error occured when converting file to mp3");
        }
        return false;
    }
}
