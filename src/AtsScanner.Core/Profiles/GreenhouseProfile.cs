using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Greenhouse uses a modern parser that handles most formats well.
/// It recognises LinkedIn and standard professional social profiles.
/// </summary>
public sealed class GreenhouseProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Greenhouse;
    public override string DisplayName => "Greenhouse";

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Multi-column layout detected.",
                "Consider switching to a single-column layout for maximum compatibility, though Greenhouse handles columns better than most ATS platforms."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Remove decorative images. Profile photos and logos are ignored and may displace parsed text."));

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Include plain-text contact information at the top of the document."));

        foreach (var missing in GetMissingSections(resume, CoreSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Add a standard '{missing}' section header."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "No summary or objective section detected.",
                "A short professional summary helps Greenhouse's keyword matching for role fit."));

        if (resume.Contact.LinkedIn is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "LinkedIn URL not detected.",
                "Greenhouse recognises LinkedIn profile URLs and can pre-populate fields from them."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, CoreSections));
    }
}
