using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YouTubeDownloader.Core;

namespace YouTubeDownloader.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IYoutubeService _service;
    private CancellationTokenSource? _cts;

    /// <summary>Raised (on the UI thread) when a download run ends, so the view can show a completion dialog.</summary>
    public event Action<DownloadCompletedInfo>? DownloadCompleted;

    public MainWindowViewModel() : this(new YoutubeService()) { }

    public MainWindowViewModel(IYoutubeService service)
    {
        _service = service;
        OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string _outputDirectory;
    [ObservableProperty] private bool _isAudio;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isPlaylist;
    [ObservableProperty] private string _playlistInfo = string.Empty;
    [ObservableProperty] private string _status = "Paste a YouTube video or playlist link to begin.";
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private StreamOption? _selectedQuality;

    public ObservableCollection<VideoItemViewModel> Videos { get; } = new();
    public ObservableCollection<StreamOption> QualityOptions { get; } = new();

    public bool HasVideos => Videos.Count > 0;

    // Reload the quality list whenever the user flips between Video and Audio.
    partial void OnIsAudioChanged(bool value)
    {
        if (HasVideos && !IsBusy)
            _ = LoadQualityOptionsAsync();
    }

    [RelayCommand]
    private async Task FetchAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Url))
        {
            Status = "Please paste a link first.";
            return;
        }

        IsBusy = true;
        OverallProgress = 0;
        try
        {
            Status = "Fetching details…";
            Videos.Clear();
            OnPropertyChanged(nameof(HasVideos));

            var resolved = await _service.ResolveAsync(Url.Trim());

            foreach (var v in resolved.Videos)
                Videos.Add(new VideoItemViewModel(v));

            IsPlaylist = resolved.IsPlaylist;
            PlaylistInfo = resolved.IsPlaylist
                ? $"Playlist: {resolved.Title}  ({resolved.Videos.Count} videos)"
                : string.Empty;
            OnPropertyChanged(nameof(HasVideos));

            await LoadQualityOptionsAsync();

            Status = resolved.IsPlaylist
                ? $"Found {resolved.Videos.Count} videos. Choose format & quality, then Download."
                : "Ready. Choose format & quality, then Download.";
        }
        catch (Exception ex)
        {
            Status = $"Couldn't fetch that link: {ex.Message}";
            IsPlaylist = false;
            PlaylistInfo = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadQualityOptionsAsync()
    {
        if (!HasVideos) return;

        var kind = IsAudio ? MediaKind.Audio : MediaKind.Video;
        var representative = Videos[0].Entry.Url;
        var options = await _service.GetStreamOptionsAsync(representative, kind);

        QualityOptions.Clear();
        foreach (var o in options)
            QualityOptions.Add(o);

        SelectedQuality = QualityOptions.FirstOrDefault();
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (IsBusy) return;

        var selected = Videos.Where(v => v.IsSelected).ToList();
        if (selected.Count == 0)
        {
            Status = "Select at least one video to download.";
            return;
        }
        if (SelectedQuality is null)
        {
            Status = "Choose a quality first.";
            return;
        }

        var kind = IsAudio ? MediaKind.Audio : MediaKind.Video;
        var quality = SelectedQuality;
        _cts = new CancellationTokenSource();

        IsBusy = true;
        OverallProgress = 0;
        var total = selected.Count;
        var completed = 0;
        var succeeded = 0;
        var cancelled = false;

        // Reset state for everything in the list and lock the rows for the run.
        foreach (var v in Videos)
        {
            v.Progress = 0;
            v.Status = v.IsSelected ? "Queued" : "Skipped";
            v.ControlsEnabled = false;
        }

        try
        {
            foreach (var item in selected)
            {
                _cts.Token.ThrowIfCancellationRequested();

                var done = completed; // capture for the progress closure
                // Guard against late Progress<double> callbacks: Report() posts to the UI thread
                // asynchronously, so a stale mid-download value can otherwise arrive *after* we mark
                // the row done and revert the bar to ~95% / "Converting to MP3…".
                var itemFinished = false;
                item.Status = "Downloading…";
                var progress = new Progress<double>(p =>
                {
                    if (itemFinished) return;
                    item.Progress = p;
                    OverallProgress = (done + p) / total;
                    if (kind == MediaKind.Audio && p >= 0.95 && p < 1.0)
                        item.Status = "Converting to MP3…";
                });

                try
                {
                    Status = total > 1
                        ? $"Downloading {done + 1} of {total}: {item.Title}"
                        : $"Downloading: {item.Title}";

                    await _service.DownloadAsync(item.Entry, kind, quality, OutputDirectory, progress, _cts.Token);

                    itemFinished = true;
                    item.Progress = 1;
                    item.Status = "Done ✓";
                    succeeded++;
                }
                catch (OperationCanceledException)
                {
                    itemFinished = true;
                    throw;
                }
                catch (Exception ex)
                {
                    itemFinished = true;
                    item.Status = $"Failed: {ex.Message}";
                }

                completed++;
                OverallProgress = (double)completed / total;
            }

            Status = $"Finished — {succeeded} of {total} downloaded to {OutputDirectory}";
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            foreach (var item in selected.Where(i => i.Status is "Queued" or "Downloading…"))
                item.Status = "Cancelled";
            Status = $"Cancelled — {succeeded} of {total} completed before stopping.";
        }
        finally
        {
            IsBusy = false;
            foreach (var v in Videos)
                v.ControlsEnabled = true;
            _cts.Dispose();
            _cts = null;

            var title = cancelled ? "Download cancelled" : "Download complete";
            var message = cancelled
                ? $"Stopped after {succeeded} of {total}.\nFiles saved so far are in the folder below."
                : $"{succeeded} of {total} downloaded successfully.";
            DownloadCompleted?.Invoke(new DownloadCompletedInfo(title, message, OutputDirectory));
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();
}

/// <summary>Payload for the completion dialog the view shows when a download run ends.</summary>
public record DownloadCompletedInfo(string Title, string Message, string Directory);
