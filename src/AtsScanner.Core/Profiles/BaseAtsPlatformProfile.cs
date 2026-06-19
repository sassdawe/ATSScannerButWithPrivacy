using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>Base helpers shared across all ATS profile implementations.</summary>
public abstract class BaseAtsPlatformProfile : IAtsPlatformProfile
{
    public abstract AtsPlatform Platform { get; }
    public abstract string DisplayName { get; }

    public abstract ScanResult Analyze(ParsedResume resume);

    protected static readonly SectionType[] CoreSections =
        [SectionType.Experience, SectionType.Education, SectionType.Skills];

    protected static IReadOnlyList<SectionType> GetDetectedSections(ParsedResume resume) =>
        resume.Sections.Select(s => s.Type).Distinct().ToList();

    protected static IReadOnlyList<SectionType> GetMissingSections(
        ParsedResume resume, SectionType[] required)
    {
        var detected = resume.Sections.Select(s => s.Type).ToHashSet();
        return required.Where(s => !detected.Contains(s)).ToList();
    }

    protected static int CalculateScore(IReadOnlyList<ScanIssue> issues)
    {
        int deductions = issues.Sum(i => i.Severity switch
        {
            IssueSeverity.Critical => 20,
            IssueSeverity.Warning => 10,
            IssueSeverity.Info => 5,
            _ => 0
        });
        return Math.Max(0, 100 - deductions);
    }

    protected static bool HasFormat(ParsedResume r, ResumeFormatFlags flag) =>
        r.Format.HasFlag(flag);

    protected static bool HasSection(ParsedResume r, SectionType type) =>
        r.Sections.Any(s => s.Type == type);

    protected static bool HasContactInfo(ParsedResume r) =>
        r.Contact.Email is not null || r.Contact.Phone is not null;

    protected static bool HasValidDates(ParsedResume r)
    {
        // Simple heuristic: look for 4-digit years in raw text
        return System.Text.RegularExpressions.Regex.IsMatch(r.RawText, @"\b(19|20)\d{2}\b");
    }
}
