using AtsScanner.Core.Models;

namespace AtsScanner.Gui.ViewModels;

/// <summary>Display-ready wrapper around a single <see cref="ScanIssue"/>.</summary>
public sealed class IssueDisplay(ScanIssue issue)
{
    public string Icon => issue.Severity switch
    {
        IssueSeverity.Critical => "✖",
        IssueSeverity.Warning => "⚠",
        _ => "ℹ"
    };

    public Color IconColor => issue.Severity switch
    {
        IssueSeverity.Critical => Colors.Crimson,
        IssueSeverity.Warning => Colors.Goldenrod,
        _ => Colors.Gray
    };

    public string Category => issue.Category;

    public string Message => issue.Message;

    public string? Suggestion => issue.Suggestion;

    public bool HasSuggestion => !string.IsNullOrEmpty(issue.Suggestion);
}
