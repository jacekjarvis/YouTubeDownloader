using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using YouTubeDownloader.Gui.ViewModels;

namespace YouTubeDownloader.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
