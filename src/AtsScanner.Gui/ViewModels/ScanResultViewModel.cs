using AtsScanner.Core.Models;

namespace AtsScanner.Gui.ViewModels;

/// <summary>Display-ready wrapper around a single platform's <see cref="ScanResult"/>, with expand/collapse state for the details panel.</summary>
public sealed class ScanResultViewModel : ObservableObject
{
    private bool _isExpanded;

    public ScanResultViewModel(ScanResult result, string displayName)
    {
        Score = result.Score;
        Rating = result.Rating;
        PlatformName = displayName;
        Issues = result.Issues
            .OrderBy(i => i.Severity)
            .Select(i => new IssueDisplay(i))
            .ToList();
        DetectedSections = result.DetectedSections.Count > 0
            ? string.Join(", ", result.DetectedSections)
            : null;
        MissingSections = result.MissingSections.Count > 0
            ? string.Join(", ", result.MissingSections)
            : null;

        var critical = result.Issues.Count(i => i.Severity == IssueSeverity.Critical);
        var warning = result.Issues.Count(i => i.Severity == IssueSeverity.Warning);
        var info = result.Issues.Count(i => i.Severity == IssueSeverity.Info);

        IssuesSummary = critical + warning + info == 0
            ? "No issues"
            : string.Join("  ", new[]
            {
                critical > 0 ? $"{critical} critical" : null,
                warning > 0 ? $"{warning} warning" : null,
                info > 0 ? $"{info} info" : null
            }.Where(s => s is not null));
    }

    public string PlatformName { get; }

    public int Score { get; }

    public string Rating { get; }

    public string ScoreLabel => $"{Score}/100";

    public Color RatingColor => Score switch
    {
        >= 90 => Colors.SeaGreen,
        >= 75 => Colors.YellowGreen,
        >= 55 => Colors.Goldenrod,
        >= 35 => Colors.DarkOrange,
        _ => Colors.Crimson
    };

    public string IssuesSummary { get; }

    public IReadOnlyList<IssueDisplay> Issues { get; }

    public bool HasIssues => Issues.Count > 0;

    public string? DetectedSections { get; }

    public bool HasDetectedSections => DetectedSections is not null;

    public string? MissingSections { get; }

    public bool HasMissingSections => MissingSections is not null;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
                OnPropertyChanged(nameof(ExpanderGlyph));
        }
    }

    public string ExpanderGlyph => IsExpanded ? "▲" : "▼";
}
