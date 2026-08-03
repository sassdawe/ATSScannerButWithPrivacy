using System.Collections.ObjectModel;
using System.Windows.Input;
using AtsScanner.Core.Analysis;
using AtsScanner.Core.Profiles;

namespace AtsScanner.Gui.ViewModels;

/// <summary>Backing view model for <see cref="MainPage"/>: file selection, platform toggles, and scan execution.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ResumeAnalyzer _analyzer = new();

    private string? _selectedFilePath;
    private bool _isBusy;
    private string _statusMessage = "Choose a resume file (.pdf, .docx, .md) to get started.";

    public MainViewModel()
    {
        Platforms = new ObservableCollection<PlatformOption>(
            ProfileRegistry.GetAll().Select(p => new PlatformOption(p.Platform, p.DisplayName)));

        Results = [];

        ScanCommand = new Command(async () => await ScanAsync(), () => CanScan);
        SelectAllCommand = new Command(() => SetAllPlatforms(true));
        SelectNoneCommand = new Command(() => SetAllPlatforms(false));
    }

    public ObservableCollection<PlatformOption> Platforms { get; }

    public ObservableCollection<ScanResultViewModel> Results { get; }

    public ICommand ScanCommand { get; }

    public ICommand SelectAllCommand { get; }

    public ICommand SelectNoneCommand { get; }

    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        private set
        {
            if (SetProperty(ref _selectedFilePath, value))
            {
                OnPropertyChanged(nameof(SelectedFileName));
                OnPropertyChanged(nameof(HasSelectedFile));
                ((Command)ScanCommand).ChangeCanExecute();
            }
        }
    }

    public string SelectedFileName =>
        SelectedFilePath is null ? "No file selected" : Path.GetFileName(SelectedFilePath);

    public bool HasSelectedFile => SelectedFilePath is not null;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                ((Command)ScanCommand).ChangeCanExecute();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private bool CanScan => !IsBusy && HasSelectedFile && Platforms.Any(p => p.IsSelected);

    public async Task PickFileAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".pdf", ".docx", ".md", ".markdown"] },
                { DevicePlatform.MacCatalyst, ["pdf", "docx", "md", "markdown"] }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a resume",
                FileTypes = customFileType
            });

            if (result is null) return;

            SelectedFilePath = result.FullPath;
            Results.Clear();
            StatusMessage = "Ready to scan.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open file picker: {ex.Message}";
        }
    }

    private void SetAllPlatforms(bool selected)
    {
        foreach (var platform in Platforms)
            platform.IsSelected = selected;

        ((Command)ScanCommand).ChangeCanExecute();
    }

    private async Task ScanAsync()
    {
        if (SelectedFilePath is null) return;

        IsBusy = true;
        Results.Clear();
        StatusMessage = $"Scanning {SelectedFileName}...";

        try
        {
            var selectedPlatforms = Platforms.Where(p => p.IsSelected).Select(p => p.Platform).ToList();

            foreach (var platform in selectedPlatforms)
            {
                var result = await _analyzer.AnalyzeAsync(SelectedFilePath, platform);
                var displayName = ProfileRegistry.Get(platform).DisplayName;
                Results.Add(new ScanResultViewModel(result, displayName));
            }

            // Highest score first, mirroring the CLI summary table ordering.
            var ordered = Results.OrderByDescending(r => r.Score).ToList();
            Results.Clear();
            foreach (var r in ordered)
                Results.Add(r);

            StatusMessage = $"Scan complete — {Results.Count} platform{(Results.Count == 1 ? "" : "s")} analysed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
