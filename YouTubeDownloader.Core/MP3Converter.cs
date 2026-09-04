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
    /// Converts <paramref name="sourcePath"/> to <paramref name="mp3Path"/> and deletes the
    /// original container on success.
    /// </summary>
    public bool Convert(string sourcePath, string mp3Path)
    {
        try
        {
            FFMpeg.ExtractAudio(sourcePath, mp3Path);
            File.Delete(sourcePath);
            return true;
        }
        catch
        {
            // Leave the original container in place so the download isn't lost.
            return false;
        }
    }
}
