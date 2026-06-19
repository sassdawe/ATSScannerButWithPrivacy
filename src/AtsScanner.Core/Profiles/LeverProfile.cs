using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Lever uses a modern parser with strong PDF support and recognises
/// developer-centric links like GitHub.
/// </summary>
public sealed class LeverProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Lever;
    public override string DisplayName => "Lever";

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Multi-column layout detected.",
                "Lever handles two-column layouts better than most ATS, but a single-column layout eliminates any parsing risk."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Formatting",
                "Images or graphics detected.",
                "Lever cannot extract text from embedded images. Ensure all important content is in text form."));

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Include contact details in plain text at the top of the document."));

        foreach (var missing in GetMissingSections(resume, CoreSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Structure",
                $"{missing} section not detected.",
                $"Add a '{missing}' section for better structured parsing."));

        if (resume.Contact.GitHub is null && resume.Contact.LinkedIn is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "No GitHub or LinkedIn URL detected.",
                "Lever recognises and auto-links GitHub and LinkedIn profile URLs — include them in your contact section."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, CoreSections));
    }
}
