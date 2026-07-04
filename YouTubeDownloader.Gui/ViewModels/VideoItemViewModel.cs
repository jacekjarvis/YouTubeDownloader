using CommunityToolkit.Mvvm.ComponentModel;
using YouTubeDownloader.Core;

namespace YouTubeDownloader.Gui.ViewModels;

/// <summary>One row in the videos list — a single video that can be (de)selected and tracks its own progress.</summary>
public partial class VideoItemViewModel : ObservableObject
{
    public VideoEntry Entry { get; }

    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status = "Queued";

    /// <summary>False while a download is running, so the row's checkbox can't be toggled mid-download.</summary>
    [ObservableProperty] private bool _controlsEnabled = true;

    public VideoItemViewModel(VideoEntry entry)
    {
        Entry = entry;
    }

    public string Title => Entry.Title;
    public string SubTitle => $"{Entry.Author}  ·  {Entry.DurationText}";
}
