using AtsScanner.Core.Models;

namespace AtsScanner.Gui.ViewModels;

/// <summary>Represents one selectable ATS platform checkbox in the platform picker.</summary>
public sealed class PlatformOption(AtsPlatform platform, string displayName) : ObservableObject
{
    private bool _isSelected = true;

    public AtsPlatform Platform { get; } = platform;

    public string DisplayName { get; } = displayName;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
