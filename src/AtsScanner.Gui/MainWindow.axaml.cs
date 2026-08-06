using System.Diagnostics;
using AtsScanner.Gui.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AtsScanner.Gui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private async void OnChooseFileClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a resume",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Resume files")
                {
                    Patterns = ["*.pdf", "*.docx", "*.md", "*.markdown"]
                }
            ]
        });

        if (files.Count == 0)
            return;

        var localPath = files[0].TryGetLocalPath();
        _viewModel.SelectFile(localPath ?? files[0].Path.LocalPath);
    }

    private void OnToggleExpandClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ScanResultViewModel result })
            result.IsExpanded = !result.IsExpanded;
    }

    private static void OnSponsorLinkPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/sponsors/sassdawe")
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore failures opening the default browser (e.g. no handler registered).
        }
    }
}
