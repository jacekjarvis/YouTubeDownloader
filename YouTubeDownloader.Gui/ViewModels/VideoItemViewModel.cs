using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YouTubeDownloader.Core;

namespace YouTubeDownloader.Gui.ViewModels;

/// <summary>Where a row is in its lifecycle. Drives both the status text and the row's icons/buttons.</summary>
public enum DownloadState
{
    Pending,      // selected, waiting its turn
    Downloading,  // actively downloading
    Converting,   // downloaded; queued for / running the mp3 conversion
    Done,         // finished successfully
    Failed,       // errored (or cancelled)
    Paused,       // user paused it; the worker skips it until resumed
    Skipped       // not selected for this run
}

/// <summary>One row in the videos list — a single video that can be (de)selected, paused, and tracks its own progress.</summary>
public partial class VideoItemViewModel : ObservableObject
{
    public VideoEntry Entry { get; }

    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private double _progress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDownloading))]
    [NotifyPropertyChangedFor(nameof(IsConverting))]
    [NotifyPropertyChangedFor(nameof(IsDone))]
    [NotifyPropertyChangedFor(nameof(PauseButtonText))]
    [NotifyPropertyChangedFor(nameof(CanTogglePause))]
    private DownloadState _state = DownloadState.Pending;

    [ObservableProperty] private string _status = "Queued";

    /// <summary>False while a download is running, so the row's checkbox can't be toggled mid-download.</summary>
    [ObservableProperty] private bool _controlsEnabled = true;

    /// <summary>True during a run for rows that can still be paused/resumed (shows the Pause button).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanTogglePause))]
    private bool _canPause;

    /// <summary>
    /// Set by the download worker while this row is actively downloading, so pausing can cancel
    /// just this one download (leaving the rest of the run going).
    /// </summary>
    public CancellationTokenSource? ActiveDownloadCts { get; set; }

    public VideoItemViewModel(VideoEntry entry)
    {
        Entry = entry;
    }

    public string Title => Entry.Title;
    public string SubTitle => $"{Entry.Author}  ·  {Entry.DurationText}";

    public bool IsDownloading => State == DownloadState.Downloading;
    public bool IsConverting => State == DownloadState.Converting;
    public bool IsDone => State == DownloadState.Done;

    public string PauseButtonText => State == DownloadState.Paused ? "Resume" : "Pause";

    public bool CanTogglePause =>
        CanPause && State is DownloadState.Pending or DownloadState.Downloading or DownloadState.Paused;

    /// <summary>Moves the row to a new state with its matching display text.</summary>
    public void SetState(DownloadState state, string status)
    {
        State = state;
        Status = status;
    }

    [RelayCommand]
    private void TogglePause()
    {
        switch (State)
        {
            case DownloadState.Pending:
                // Not started yet — just hold it; the worker will skip it.
                SetState(DownloadState.Paused, "Paused");
                break;
            case DownloadState.Downloading:
                // Cancel the in-flight download; the worker re-queues it as Paused.
                ActiveDownloadCts?.Cancel();
                break;
            case DownloadState.Paused:
                // Put it back in the queue; the worker picks it up again.
                Progress = 0;
                SetState(DownloadState.Pending, "Queued");
                break;
        }
    }
}
