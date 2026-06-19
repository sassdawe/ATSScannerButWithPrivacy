using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Taleo is one of the oldest and strictest ATS parsers.
/// It is notorious for mangling resumes with any non-trivial formatting.
/// Plain text or simple single-column Word documents fare best.
/// </summary>
public sealed class TaleoProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Taleo;
    public override string DisplayName => "Taleo (Oracle)";

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Formatting",
                "Multi-column layout detected.",
                "Taleo's parser will merge column text left-to-right, completely scrambling your resume. Use a strict single-column layout."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Formatting",
                "Tables detected.",
                "Taleo struggles severely with tables — cells are often read in the wrong order or dropped entirely. Replace all tables with bullet lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Remove all images. Taleo cannot read text in or around image elements."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Document headers or footers detected.",
                "Taleo frequently ignores header/footer content. Move all contact details into the document body."));

        if (HasFormat(resume, ResumeFormatFlags.HasSpecialCharacters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Special or decorative characters detected.",
                "Replace bullet symbols (•, ◆, ▶) with plain hyphens or asterisks. Taleo may drop or garble Unicode characters."));

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Place plain-text contact information at the very top of the document."));

        foreach (var missing in GetMissingSections(resume, CoreSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Use an exact, standard header such as 'Work Experience', 'Education', or 'Skills'. Taleo maps parsed blocks to known field names."));

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Use explicit date ranges in 'Month YYYY' or 'MM/YYYY' format. Taleo's date extractor is strict."));

        if (resume.FileFormat == "pdf")
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Format",
                "PDF format detected.",
                "Older versions of Taleo parse .docx more reliably than PDF. If possible, submit a Word document."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, CoreSections));
    }
}
