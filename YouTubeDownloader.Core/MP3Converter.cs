using FFMpegCore;

namespace YouTubeDownloader.Core;

/// <summary>Extracts an mp3 track from a downloaded audio container using ffmpeg.</summary>
public class MP3Converter
{
    public MP3Converter(string ffmpegPath)
    {
        GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
    }

    /// <summary>
    /// Converts "{source}.{fileType}" to "{source}.mp3" and deletes the original on success.
    /// </summary>
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
            // Leave the original container in place so the download isn't lost.
            return false;
        }
    }
}
