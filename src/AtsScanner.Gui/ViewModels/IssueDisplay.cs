using AtsScanner.Core.Models;

using Avalonia.Media;

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

    public IBrush IconBrush => issue.Severity switch
    {
        IssueSeverity.Critical => Brushes.Crimson,
        IssueSeverity.Warning => Brushes.Goldenrod,
        _ => Brushes.Gray
    };

    public string Category => issue.Category;

    public string Message => issue.Message;

    public string? Suggestion => issue.Suggestion;

    public bool HasSuggestion => !string.IsNullOrEmpty(issue.Suggestion);
}
