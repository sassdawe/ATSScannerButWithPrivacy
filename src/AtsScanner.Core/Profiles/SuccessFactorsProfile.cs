using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// SAP SuccessFactors shares many parsing characteristics with Workday —
/// it dislikes complex visual layouts and expects conventional section headers.
/// </summary>
public sealed class SuccessFactorsProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.SuccessFactors;
    public override string DisplayName => "SAP SuccessFactors";

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Formatting",
                "Multi-column layout detected.",
                "SuccessFactors reads linearly and will jumble text from side-by-side columns. Use a single-column layout."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Replace tables with plain text or bullet-point lists. Table cells may be read out of order."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Remove all images. SuccessFactors cannot extract text from image content."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Document headers or footers detected.",
                "SuccessFactors may ignore header/footer content. Keep all contact information in the document body."));

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Include plain-text contact information at the top of the document body."));

        foreach (var missing in GetMissingSections(resume, CoreSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Use a standard section header such as 'Work Experience', 'Education', or 'Skills'."));

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Use explicit date ranges in 'Month YYYY' format. SuccessFactors maps dates to structured job history fields."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "No summary section detected.",
                "A professional summary helps SuccessFactors populate the candidate profile overview field."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, CoreSections));
    }
}
