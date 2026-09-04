using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YouTubeDownloader.Core;

namespace YouTubeDownloader.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// How many videos to try when building the quality list. A playlist can open with an entry
    /// that's private, removed or region-blocked; probing a few more keeps the dropdown populated
    /// instead of leaving the user staring at an empty list.
    /// </summary>
    private const int QualityProbeLimit = 5;

    private readonly IYoutubeService _service;
    private CancellationTokenSource? _cts;

    /// <summary>Raised (on the UI thread) when a download run ends, so the view can show a completion dialog.</summary>
    public event Action<DownloadCompletedInfo>? DownloadCompleted;

    public MainWindowViewModel() : this(new YoutubeService()) { }

    public MainWindowViewModel(IYoutubeService service)
    {
        _service = service;
        OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // The step-by-step UI keys off "are there results?" and "is a quality chosen?", so both
        // collections have to push those derived flags whenever they change.
        Videos.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasVideos));
            OnPropertyChanged(nameof(ShowDownloadButton));
            OnPropertyChanged(nameof(ShowQualityRetry));
        };
        QualityOptions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasQualityOptions));
            OnPropertyChanged(nameof(ShowQualityRetry));
            OnPropertyChanged(nameof(ShowPlaylistProgress));
            OnPropertyChanged(nameof(QualityPlaceholder));
        };
    }

    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string _outputDirectory;
    [ObservableProperty] private bool _isAudio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQualityRetry))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlaylistProgress))]
    private bool _isPlaylist;
    [ObservableProperty] private string _playlistInfo = string.Empty;
    [ObservableProperty] private string _status = "Paste a YouTube video or playlist link, then press Fetch.";
    [ObservableProperty] private double _overallProgress;

    /// <summary>True while the quality list is being read, so the dropdown can say so.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QualityPlaceholder))]
    private bool _isLoadingQualities;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDownloadButton))]
    private StreamOption? _selectedQuality;

    public ObservableCollection<VideoItemViewModel> Videos { get; } = new();
    public ObservableCollection<StreamOption> QualityOptions { get; } = new();

    /// <summary>Step 2 of the UI (format, folder, quality) only exists once a fetch returned videos.</summary>
    public bool HasVideos => Videos.Count > 0;

    public bool HasQualityOptions => QualityOptions.Count > 0;

    /// <summary>Step 3: the Download button appears only once a quality has actually been chosen.</summary>
    public bool ShowDownloadButton => HasVideos && SelectedQuality is not null;

    /// <summary>Offer a retry when the quality probe came back empty (e.g. a transient YouTube error).</summary>
    public bool ShowQualityRetry => HasVideos && !HasQualityOptions && !IsBusy;

    /// <summary>Playlist progress and the per-video quality note only matter once downloading is possible.</summary>
    public bool ShowPlaylistProgress => IsPlaylist && HasQualityOptions;

    public string QualityPlaceholder =>
        IsLoadingQualities ? "Reading available qualities…"
        : HasQualityOptions ? "Choose a quality…"
        : "No qualities available — press Retry";

    // Reload the quality list whenever the user flips between Video and Audio.
    partial void OnIsAudioChanged(bool value)
    {
        if (HasVideos && !IsBusy)
            _ = ReloadQualitiesAsync();
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
            ClearResults();

            var resolved = await _service.ResolveAsync(Url.Trim());

            if (resolved.Videos.Count == 0)
            {
                Status = "That link resolved, but it contains no videos.";
                return;
            }

            foreach (var v in resolved.Videos)
                Videos.Add(new VideoItemViewModel(v));

            IsPlaylist = resolved.IsPlaylist;
            PlaylistInfo = resolved.IsPlaylist
                ? $"Playlist: {resolved.Title}  ({resolved.Videos.Count} videos)"
                : string.Empty;

            // A failure in there leaves its own explanation in Status — don't paint over it.
            if (!await LoadQualityOptionsAsync())
                return;

            Status = resolved.IsPlaylist
                ? $"Found {resolved.Videos.Count} videos. Choose a quality, then Download."
                : "Ready. Choose a quality, then Download.";
        }
        catch (Exception ex)
        {
            ClearResults();
            Status = $"Couldn't fetch that link: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Re-reads the quality list on its own (format switch, or the Retry button).</summary>
    [RelayCommand]
    private async Task ReloadQualitiesAsync()
    {
        if (IsBusy || !HasVideos) return;

        IsBusy = true;
        try
        {
            if (await LoadQualityOptionsAsync())
                Status = "Ready. Choose a quality, then Download.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Fills <see cref="QualityOptions"/> from the first video that will give up a stream manifest.
    /// Returns false (with an explanation in <see cref="Status"/>) if none of them will.
    /// </summary>
    private async Task<bool> LoadQualityOptionsAsync()
    {
        QualityOptions.Clear();
        SelectedQuality = null;

        if (!HasVideos) return false;

        var kind = IsAudio ? MediaKind.Audio : MediaKind.Video;
        IsLoadingQualities = true;
        try
        {
            Status = "Reading available qualities…";

            string? lastError = null;
            foreach (var candidate in Videos.Take(QualityProbeLimit))
            {
                try
                {
                    var options = await _service.GetStreamOptionsAsync(candidate.Entry.Url, kind);
                    if (options.Count == 0)
                    {
                        var what = kind == MediaKind.Audio ? "audio" : "video";
                        lastError = $"\"{candidate.Title}\" has no downloadable {what} streams.";
                        continue;
                    }

                    foreach (var o in options)
                        QualityOptions.Add(o);

                    SelectedQuality = QualityOptions[0];
                    return true;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }

            Status = $"Couldn't read the quality list — {lastError} " +
                     "It may be private, removed or region-blocked. Press Retry to try again.";
            return false;
        }
        finally
        {
            IsLoadingQualities = false;
        }
    }

    private void ClearResults()
    {
        Videos.Clear();
        QualityOptions.Clear();
        SelectedQuality = null;
        IsPlaylist = false;
        PlaylistInfo = string.Empty;
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
        var ct = _cts.Token;

        IsBusy = true;
        OverallProgress = 0;
        var total = selected.Count;
        var cancelled = false;

        // Reset state for everything in the list and lock the rows for the run.
        foreach (var v in Videos)
        {
            v.Progress = 0;
            v.ActiveDownloadCts = null;
            v.ControlsEnabled = false;
            if (v.IsSelected)
            {
                v.SetState(DownloadState.Pending, "Queued");
                v.CanPause = true;
            }
            else
            {
                v.SetState(DownloadState.Skipped, "Skipped");
                v.CanPause = false;
            }
        }

        // Audio runs the mp3 conversions on a background worker fed by this queue; video has no
        // separate conversion stage, so it doesn't need one.
        Channel<ConversionJob>? channel = null;
        Task? converter = null;
        if (kind == MediaKind.Audio)
        {
            channel = Channel.CreateUnbounded<ConversionJob>();
            converter = Task.Run(() => ConsumeConversionsAsync(channel.Reader, selected, ct));
        }

        try
        {
            // Keep pulling the next eligible row until everything's resolved. Paused rows are
            // skipped; if only paused rows remain, idle until the user resumes one (or cancels).
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var next = selected.FirstOrDefault(v => v.State == DownloadState.Pending);
                if (next is null)
                {
                    if (selected.Any(v => v.State == DownloadState.Paused))
                    {
                        Status = "Paused — resume items to continue, or press Cancel.";
                        await Task.Delay(250, ct);
                        continue;
                    }
                    break; // nothing left downloading, converting or waiting
                }

                await DownloadOneAsync(next, kind, quality, channel, selected, ct);
            }

            if (kind == MediaKind.Audio)
                Status = "Finishing conversions…";
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            foreach (var item in selected.Where(IsStillActive))
                item.SetState(DownloadState.Failed, "Cancelled");
        }
        finally
        {
            // Close the conversion queue and let the worker drain (it's a no-op for video).
            channel?.Writer.Complete();
            if (converter is not null)
            {
                try { await converter; }
                catch { /* the worker swallows its own errors; nothing to surface here */ }
            }

            IsBusy = false;
            Url = string.Empty; // clear the address box now the run is finished
            foreach (var v in Videos)
            {
                v.ControlsEnabled = true;
                v.CanPause = false;
                v.ActiveDownloadCts = null;
            }
            _cts.Dispose();
            _cts = null;

            var ok = CompletedCount();
            Status = cancelled
                ? $"Cancelled — {ok} of {total} completed before stopping."
                : $"Finished — {ok} of {total} downloaded to {OutputDirectory}";

            var title = cancelled ? "Download cancelled" : "Download complete";
            var message = cancelled
                ? $"Stopped after {ok} of {total}.\nFiles saved so far are in the folder below."
                : $"{ok} of {total} downloaded successfully.";
            DownloadCompleted?.Invoke(new DownloadCompletedInfo(title, message, OutputDirectory));
        }
    }

    // Downloads a single row. Audio hands the finished container to the conversion worker and
    // returns immediately (so the next download can start); video downloads+muxes in one go.
    // A per-row cancellation source lets the user pause just this download and re-queue it.
    private async Task DownloadOneAsync(
        VideoItemViewModel item, MediaKind kind, StreamOption quality,
        Channel<ConversionJob>? channel, List<VideoItemViewModel> selected, CancellationToken ct)
    {
        using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        item.ActiveDownloadCts = itemCts;
        var itemCt = itemCts.Token;

        var finished = false; // guard against late Progress<double> callbacks reverting the row
        item.SetState(DownloadState.Downloading, "Downloading…");
        var progress = new Progress<double>(p =>
        {
            if (finished) return;
            // For audio, downloads own the first 95% of the bar and conversion fills the rest.
            item.Progress = kind == MediaKind.Audio ? p * 0.95 : p;
            UpdateOverall(selected);
        });

        try
        {
            Status = $"Downloading: {item.Title}";

            if (kind == MediaKind.Audio)
            {
                var containerPath = await _service.DownloadAudioContainerAsync(item.Entry, quality, OutputDirectory, progress, itemCt);
                finished = true;
                item.Progress = 0.95;
                item.SetState(DownloadState.QueuedForConversion, "Queued for MP3…");
                UpdateOverall(selected);
                await channel!.Writer.WriteAsync(new ConversionJob(item, containerPath), CancellationToken.None);
            }
            else
            {
                await Task.Run(() => _service.DownloadAsync(item.Entry, MediaKind.Video, quality, OutputDirectory, progress, itemCt), itemCt);
                finished = true;
                item.Progress = 1;
                item.SetState(DownloadState.Done, "Done ✓");
                UpdateOverall(selected);
            }
        }
        catch (OperationCanceledException)
        {
            finished = true;
            if (ct.IsCancellationRequested)
                throw; // whole run was cancelled — let it bubble up
            // Otherwise the user paused just this row; put it back so it can be resumed.
            item.Progress = 0;
            item.SetState(DownloadState.Paused, "Paused");
            UpdateOverall(selected);
        }
        catch (Exception ex)
        {
            finished = true;
            item.SetState(DownloadState.Failed, $"Failed: {ex.Message}");
            UpdateOverall(selected);
        }
        finally
        {
            item.ActiveDownloadCts = null;
        }
    }

    private async Task ConsumeConversionsAsync(ChannelReader<ConversionJob> reader, List<VideoItemViewModel> selected, CancellationToken ct)
    {
        await foreach (var job in reader.ReadAllAsync())
        {
            if (ct.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() => job.Item.SetState(DownloadState.Failed, "Cancelled"));
                continue;
            }

            await Dispatcher.UIThread.InvokeAsync(() => job.Item.SetState(DownloadState.Converting, "Converting to MP3…"));
            _service.ConvertToMp3(job.ContainerPath); // blocking ffmpeg work, on this background thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                job.Item.Progress = 1;
                job.Item.SetState(DownloadState.Done, "Done ✓");
                UpdateOverall(selected);
            });
        }
    }

    private void UpdateOverall(List<VideoItemViewModel> selected)
        => OverallProgress = selected.Count == 0 ? 0 : selected.Sum(v => v.Progress) / selected.Count;

    private int CompletedCount() => Videos.Count(v => v.State == DownloadState.Done);

    private static bool IsStillActive(VideoItemViewModel item)
        => item.State is DownloadState.Pending or DownloadState.Downloading
            or DownloadState.QueuedForConversion or DownloadState.Converting or DownloadState.Paused;

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    private sealed record ConversionJob(VideoItemViewModel Item, string ContainerPath);
}

/// <summary>Payload for the completion dialog the view shows when a download run ends.</summary>
public record DownloadCompletedInfo(string Title, string Message, string Directory);
