namespace AtsScanner.Core.Models;

public sealed record ScanIssue(
    IssueSeverity Severity,
    string Category,
    string Message,
    string? Suggestion = null
);
