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
        var ct = _cts.Token;

        IsBusy = true;
        OverallProgress = 0;
        var total = selected.Count;
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
            if (kind == MediaKind.Audio)
                await RunAudioPipelineAsync(selected, quality, ct);
            else
                await RunVideoDownloadsAsync(selected, quality, ct);

            var ok = CompletedCount();
            Status = $"Finished — {ok} of {total} downloaded to {OutputDirectory}";
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            foreach (var item in selected.Where(IsStillPending))
                item.Status = "Cancelled";
            Status = $"Cancelled — {CompletedCount()} of {total} completed before stopping.";
        }
        finally
        {
            IsBusy = false;
            Url = string.Empty; // clear the address box now the run is finished
            foreach (var v in Videos)
                v.ControlsEnabled = true;
            _cts.Dispose();
            _cts = null;

            var ok = CompletedCount();
            var title = cancelled ? "Download cancelled" : "Download complete";
            var message = cancelled
                ? $"Stopped after {ok} of {total}.\nFiles saved so far are in the folder below."
                : $"{ok} of {total} downloaded successfully.";
            DownloadCompleted?.Invoke(new DownloadCompletedInfo(title, message, OutputDirectory));
        }
    }

    // Video downloads mux audio+video through ffmpeg as they stream, so there's no separate
    // conversion stage; run them one at a time. Task.Run keeps any synchronous ffmpeg work off
    // the UI thread so the window stays responsive.
    private async Task RunVideoDownloadsAsync(List<VideoItemViewModel> selected, StreamOption quality, CancellationToken ct)
    {
        var index = 0;
        foreach (var item in selected)
        {
            ct.ThrowIfCancellationRequested();
            index++;

            var finished = false; // guard against late Progress<double> callbacks reverting the row
            item.Status = "Downloading…";
            var progress = new Progress<double>(p =>
            {
                if (finished) return;
                item.Progress = p;
                UpdateOverall(selected);
            });

            try
            {
                Status = selected.Count > 1
                    ? $"Downloading {index} of {selected.Count}: {item.Title}"
                    : $"Downloading: {item.Title}";

                await Task.Run(() => _service.DownloadAsync(item.Entry, MediaKind.Video, quality, OutputDirectory, progress, ct), ct);

                finished = true;
                item.Progress = 1;
                item.Status = "Done ✓";
            }
            catch (OperationCanceledException)
            {
                finished = true;
                throw;
            }
            catch (Exception ex)
            {
                finished = true;
                item.Status = $"Failed: {ex.Message}";
            }

            UpdateOverall(selected);
        }
    }

    // Audio: download each file back-to-back (network-bound, non-blocking) and hand each finished
    // container to a single background worker that runs the mp3 conversions. Downloads never wait
    // on a conversion, and the (synchronous) ffmpeg work stays off the UI thread.
    private async Task RunAudioPipelineAsync(List<VideoItemViewModel> selected, StreamOption quality, CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<ConversionJob>();
        var converter = Task.Run(() => ConsumeConversionsAsync(channel.Reader, selected, ct));

        try
        {
            var index = 0;
            foreach (var item in selected)
            {
                ct.ThrowIfCancellationRequested();
                index++;

                var finished = false; // guard against late download progress reverting the row
                item.Status = "Downloading…";
                // Downloads own the first 95% of each row's bar; the mp3 conversion fills the rest.
                var progress = new Progress<double>(p =>
                {
                    if (finished) return;
                    item.Progress = p * 0.95;
                    UpdateOverall(selected);
                });

                try
                {
                    Status = selected.Count > 1
                        ? $"Downloading {index} of {selected.Count}: {item.Title}"
                        : $"Downloading: {item.Title}";

                    var containerPath = await _service.DownloadAudioContainerAsync(item.Entry, quality, OutputDirectory, progress, ct);

                    finished = true;
                    item.Progress = 0.95;
                    item.Status = "Queued for MP3…";
                    UpdateOverall(selected);

                    await channel.Writer.WriteAsync(new ConversionJob(item, containerPath), CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    finished = true;
                    throw;
                }
                catch (Exception ex)
                {
                    finished = true;
                    item.Status = $"Failed: {ex.Message}";
                    UpdateOverall(selected);
                }
            }
        }
        finally
        {
            // Always close the queue and let the worker drain, even on cancellation, so it never hangs.
            channel.Writer.Complete();
            await converter;
        }
    }

    private async Task ConsumeConversionsAsync(ChannelReader<ConversionJob> reader, List<VideoItemViewModel> selected, CancellationToken ct)
    {
        await foreach (var job in reader.ReadAllAsync())
        {
            if (ct.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() => job.Item.Status = "Cancelled");
                continue;
            }

            await Dispatcher.UIThread.InvokeAsync(() => job.Item.Status = "Converting to MP3…");
            _service.ConvertToMp3(job.ContainerPath); // blocking ffmpeg work, on this background thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                job.Item.Progress = 1;
                job.Item.Status = "Done ✓";
                UpdateOverall(selected);
            });
        }
    }

    private void UpdateOverall(List<VideoItemViewModel> selected)
        => OverallProgress = selected.Count == 0 ? 0 : selected.Sum(v => v.Progress) / selected.Count;

    private int CompletedCount() => Videos.Count(v => v.Status == "Done ✓");

    private static bool IsStillPending(VideoItemViewModel item)
        => item.Status is "Queued" or "Downloading…" or "Queued for MP3…" or "Converting to MP3…";

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    private sealed record ConversionJob(VideoItemViewModel Item, string ContainerPath);
}

/// <summary>Payload for the completion dialog the view shows when a download run ends.</summary>
public record DownloadCompletedInfo(string Title, string Message, string Directory);
