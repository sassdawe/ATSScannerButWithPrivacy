using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Workday's parser is XML-based and struggles with complex visual layouts.
/// Multi-column formats and tables are the most common causes of data loss.
/// </summary>
public sealed class WorkdayProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Workday;
    public override string DisplayName => "Workday";

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Formatting",
                "Multi-column layout detected.",
                "Use a single-column layout. Workday's XML parser reads left-to-right, top-to-bottom and will scramble content across columns."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Replace tables with plain text or bullet points. Table cells are often parsed out of order."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Remove images and graphics. Workday cannot extract text from image content."));

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Ensure your contact information is plain text at the top of the document."));

        foreach (var missing in GetMissingSections(resume, CoreSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Add a clearly labelled '{missing}' section using a standard header."));

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "No clearly formatted dates detected.",
                "Use consistent date formats such as 'Jan 2020 – Mar 2023' or 'MM/YYYY'."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Formatting",
                "Document headers or footers detected.",
                "Move contact information from headers/footers into the document body. Workday may not parse header/footer content."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, CoreSections));
    }
}
