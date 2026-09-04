# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Solution targets **.NET 10** (`net10.0`). All commands run from the repo root.

```bash
# Build everything
dotnet build YouTubeDownloader.sln

# Run the Avalonia desktop GUI (primary interface)
dotnet run --project YouTubeDownloader.Gui

# Run the console app
dotnet run --project YouTubeDownloader

# Publish the console app as a self-contained single-file exe (win-x64)
dotnet publish YouTubeDownloader/YouTubeDownloader.csproj -c Release

# Publish the GUI as ONE portable, self-contained exe (win-x64) with ffmpeg bundled inside.
# Output: YouTubeDownloader.Gui/bin/Release/net10.0/win-x64/publish/JarvoYTDownloader.exe
# Runs on any Windows PC with no .NET install; the logo (Icon.ico) is the exe/desktop icon.
dotnet publish YouTubeDownloader.Gui/YouTubeDownloader.Gui.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:DebugType=none
```

There is currently **no test project** in this repo.

The app icon lives at repo root as `Icon.png`; `YouTubeDownloader.Gui/Assets/Icon.png` is the
in-app copy (window + header) and `YouTubeDownloader.Gui/Icon.ico` (generated from it) is the
`<ApplicationIcon>` embedded in the exe.

## Architecture

Three projects, one solution. The design rule is that **all download logic lives in `YouTubeDownloader.Core`**; the GUI and console are thin front-ends over it. When changing behavior, change Core — don't duplicate logic in a UI.

- **`YouTubeDownloader.Core`** — class library, the single source of truth.
  - `IYoutubeService` / `YoutubeService` wrap YoutubeExplode. Three operations: `ResolveAsync` (URL → video or playlist), `GetStreamOptionsAsync` (quality list, highest first), `DownloadAsync` (one video → file, with progress).
  - `MP3Converter` wraps FFMpegCore for audio→mp3 extraction.
  - Models in `Models.cs`: `MediaKind`, `VideoEntry`, `ResolvedUrl`, `StreamOption`.
- **`YouTubeDownloader.Gui`** — Avalonia 12 MVVM app (CommunityToolkit.Mvvm source generators; `[ObservableProperty]`/`[RelayCommand]`). `MainWindowViewModel` drives fetch→select→download; `VideoItemViewModel` is one row with its own selection state and progress. The folder picker is handled in `MainWindow.axaml.cs` code-behind (needs `StorageProvider`), everything else is bound.
- **`YouTubeDownloader`** — console app; `Program.Main` → `YoutubeDownloaderApp` which consumes `IYoutubeService`.

### Key design points (require reading several files to see)

- **Quality is a *target*, matched per video.** Because every video in a playlist exposes different streams, `StreamOption` carries a `TargetHeight` (video) or `TargetBitrate` (audio), or `IsHighest`. The UI builds the dropdown from the first video whose manifest it can actually read (`QualityProbeLimit` attempts — a playlist can open with a private or removed entry), but `DownloadAsync` re-fetches each video's manifest and picks the closest available stream via `PickVideo`/`PickAudio`. Don't assume the listed sizes apply to every playlist entry.

- **The GUI is a three-step reveal, driven by derived flags on the view model.** Only the URL box and Fetch exist until `HasVideos`; format/folder/quality appear then; the Download button itself is gated on `ShowDownloadButton` (`HasVideos && SelectedQuality is not null`) and sits in the same row as the quality dropdown. `ShowQualityRetry` surfaces a Retry button when the quality probe came back empty. These are plain computed properties, so anything that mutates `Videos` or `QualityOptions` has to raise their change notifications — the constructor hooks both collections' `CollectionChanged` for exactly that. A quality-load failure must leave its own message in `Status`; `FetchAsync` bails out early rather than overwriting it (that silent overwrite was the original "Download does nothing" bug).
- **Playlist-vs-video resolution:** `ResolveAsync` treats a URL as a playlist whenever `PlaylistId.TryParse` succeeds — so a `watch?v=...&list=...` link becomes the whole playlist (the UI lets the user deselect individual videos).
- **Progress:** flows through `IProgress<double>` (0.0–1.0). For audio, the download is scaled to 0–0.95 to reserve the tail for the progress-less mp3 conversion; video mux progress comes straight from YoutubeExplode.Converter.
- **Audio containers get a `.audio-part` marker.** YouTube's audio-only streams are usually mp4, so `DownloadAudioContainerAsync` writes `{title}.audio-part.mp4`, not `{title}.mp4` — otherwise an MP3 run would overwrite an MP4 of the same clip in the same folder and then delete it after extraction. `ConvertToMp3` strips the marker back off when naming the mp3.

- **ffmpeg dependency:** `ffmpeg.exe` lives in `YouTubeDownloader/ffmpeg/` and is copied to each app's output under `ffmpeg/`. `YoutubeService` defaults its ffmpeg dir to `AppContext.BaseDirectory/ffmpeg`. Video muxing passes the full exe path via `ConversionRequestBuilder.SetFFmpegPath`; mp3 extraction uses FFMpegCore's `GlobalFFOptions.BinaryFolder` (the folder, not the exe). The GUI links the same ffmpeg.exe from the console project rather than keeping a second copy.
