using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using YouTubeDownloader.Gui.ViewModels;

namespace YouTubeDownloader.Gui.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _subscribedVm;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedVm is not null)
            _subscribedVm.DownloadCompleted -= OnDownloadCompleted;

        _subscribedVm = DataContext as MainWindowViewModel;

        if (_subscribedVm is not null)
            _subscribedVm.DownloadCompleted += OnDownloadCompleted;
    }

    private async void OnDownloadCompleted(DownloadCompletedInfo info)
    {
        var dialog = BuildCompletionDialog(info);
        await dialog.ShowDialog(this);
    }

    private Window BuildCompletionDialog(DownloadCompletedInfo info)
    {
        var dialog = new Window
        {
            Title = info.Title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var openButton = new Button
        {
            Content = "Open folder",
            MinWidth = 110,
            FontWeight = FontWeight.Bold,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        openButton.Classes.Add("accent");
        openButton.Click += (_, _) =>
        {
            OpenFolder(info.Directory);
            dialog.Close();
        };

        var closeButton = new Button
        {
            Content = "Close",
            MinWidth = 90,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        closeButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = info.Title,
                    FontSize = 18,
                    FontWeight = FontWeight.Bold
                },
                new TextBlock
                {
                    Text = info.Message,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.85
                },
                new TextBlock
                {
                    Text = info.Directory,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Opacity = 0.6
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { closeButton, openButton }
                }
            }
        };

        return dialog;
    }

    private static void OpenFolder(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
        }
        catch
        {
            // Opening the folder is a convenience; ignore if the shell can't launch it.
        }
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose download folder",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        var path = folder?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
            vm.OutputDirectory = path;
    }
}
