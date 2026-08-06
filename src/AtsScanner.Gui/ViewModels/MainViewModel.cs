using System.Collections.ObjectModel;
using System.Windows.Input;
using AtsScanner.Core.Analysis;
using AtsScanner.Core.Profiles;

namespace AtsScanner.Gui.ViewModels;

/// <summary>Backing view model for <see cref="MainWindow"/>: file selection, platform toggles, and scan execution.</summary>
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

        foreach (var platform in Platforms)
            platform.PropertyChanged += (_, _) => NotifyCanScanChanged();

        Results = [];

        ScanCommand = new AsyncRelayCommand(ScanAsync, () => CanScan);
        SelectAllCommand = new RelayCommand(() => SetAllPlatforms(true));
        SelectNoneCommand = new RelayCommand(() => SetAllPlatforms(false));
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
                NotifyCanScanChanged();
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
                NotifyCanScanChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private bool CanScan => !IsBusy && HasSelectedFile && Platforms.Any(p => p.IsSelected);

    public void SelectFile(string selectedFilePath)
    {
        SelectedFilePath = selectedFilePath;
        Results.Clear();
        StatusMessage = "Ready to scan.";
    }

    private void SetAllPlatforms(bool selected)
    {
        foreach (var platform in Platforms)
            platform.IsSelected = selected;

        NotifyCanScanChanged();
    }

    private void NotifyCanScanChanged()
    {
        if (ScanCommand is AsyncRelayCommand scanCommand)
            scanCommand.NotifyCanExecuteChanged();
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
