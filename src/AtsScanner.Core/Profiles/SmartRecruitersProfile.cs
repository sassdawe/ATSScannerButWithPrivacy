using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// SmartRecruiters is a modern cloud-based ATS used by many mid-size and enterprise companies.
/// It has a capable parser with OCR fallback for image-based PDFs, but text-based documents
/// always produce the most reliable extraction results.
///
/// Key parsing characteristics:
/// - Handles PDF and DOCX well; text-layer PDFs are strongly preferred over scanned images.
/// - OCR is available but accuracy drops noticeably compared to native text extraction.
/// - Multi-column layouts frequently cause field merging or ordering errors.
/// - Tables disrupt section detection — convert table-based skills/experience to bullet lists.
/// - Recognises LinkedIn profile URLs and can pre-populate candidate profile fields.
/// - GitHub URLs are parsed and surfaced on the candidate record.
/// - Contact information (email, phone, location) should appear in plain text in the document body;
///   data in headers/footers may be missed.
/// - Standard English section headers (Experience, Education, Skills, Summary) are detected
///   reliably. Non-standard or language-specific labels reduce accuracy.
/// - Dates in MM/YYYY or Month YYYY formats parse correctly; ambiguous formats (e.g. 01/03)
///   may be interpreted incorrectly due to locale differences.
/// - Skills extraction works best with a dedicated Skills section containing comma- or
///   newline-separated keyword lists.
/// </summary>
public sealed class SmartRecruitersProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.SmartRecruiters;
    public override string DisplayName => "SmartRecruiters";

    private static readonly SectionType[] RequiredSections =
        [SectionType.Experience, SectionType.Education, SectionType.Skills];

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        // ── Formatting ────────────────────────────────────────────────────────

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Multi-column layout detected.",
                "SmartRecruiters' parser reads left-to-right line by line, so multi-column layouts often merge content from separate columns or produce garbled field order. Switch to a single-column layout."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Table cells are commonly concatenated in the wrong order or skipped entirely by SmartRecruiters' parser. Replace table-based layouts (e.g. skill grids, two-column experience blocks) with plain bullet-point lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "SmartRecruiters applies OCR to image content, but accuracy is lower than native text extraction. Any text embedded in images — skill bars, infographic sections, profile photo labels — should be replaced with plain text equivalents."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Content detected in document headers or footers.",
                "SmartRecruiters may not extract text from document headers and footers reliably. Move contact information and your name into the main body of the document."));

        // ── Contact information ───────────────────────────────────────────────

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "SmartRecruiters auto-populates candidate profile fields from the resume. Include your email address and phone number as plain text so they map correctly to the candidate record."));

        if (resume.Contact.LinkedIn is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "LinkedIn URL not detected.",
                "SmartRecruiters recognises LinkedIn profile URLs (linkedin.com/in/yourname) and links them to the candidate's SmartProfile. This helps recruiters verify your background quickly."));

        if (resume.Contact.GitHub is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "GitHub URL not detected.",
                "SmartRecruiters parses GitHub profile URLs and surfaces them on the candidate record. For technical roles, including your GitHub URL (github.com/yourname) is worth adding."));

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Add a clearly labelled '{missing}' section using a standard English header. SmartRecruiters reliably recognises 'Experience', 'Education', and 'Skills'; non-standard or translated labels reduce parsing accuracy."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "No summary or objective section detected.",
                "A short professional summary at the top of your resume gives SmartRecruiters' keyword-matching more signal for role fit and increases your visibility in recruiter searches."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "SmartRecruiters extracts employment and education timelines from date ranges. Use unambiguous formats such as 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY' to ensure correct parsing of your career timeline."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
