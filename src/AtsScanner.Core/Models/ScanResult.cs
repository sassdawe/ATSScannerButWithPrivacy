namespace AtsScanner.Core.Models;

public sealed record ScanResult(
    AtsPlatform Platform,
    int Score,
    IReadOnlyList<ScanIssue> Issues,
    IReadOnlyList<SectionType> DetectedSections,
    IReadOnlyList<SectionType> MissingSections
)
{
    /// <summary>Human-readable rating derived from the score.</summary>
    public string Rating => Score switch
    {
        >= 90 => "Excellent",
        >= 75 => "Good",
        >= 55 => "Fair",
        >= 35 => "Poor",
        _ => "Very Poor"
    };
}
