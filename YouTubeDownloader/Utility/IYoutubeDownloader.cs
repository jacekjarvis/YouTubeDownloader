using System.Collections.Generic;
using System.Threading.Tasks;

public interface IYoutubeDownloader
{
    public string VideoUrl { get; set; }

    public string GetVideoTitle();
    public IEnumerable<string> GetVideoOptions();
    public IEnumerable<string> GetAudioOptions();
    public Task DownloadMedia(char mediaType, int option, string outputPath);
    
}
