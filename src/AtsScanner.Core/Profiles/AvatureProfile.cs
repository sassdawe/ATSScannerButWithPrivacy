using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Avature is a highly configurable enterprise CRM and ATS platform used primarily by
/// Fortune 500 companies and large staffing firms. Organisations deploy it with their own
/// custom career portals (typically at careers.company.com or company.avature.net).
///
/// Key parsing characteristics:
/// - Accepts PDF and DOCX; text-based PDFs produce the most reliable extraction.
/// - Because each Avature deployment is configured by the employer, parsing behaviour can
///   differ across companies — these rules reflect the platform defaults and common setups.
/// - Multi-column layouts frequently cause field merging; single-column is strongly preferred.
/// - Tables disrupt section detection and cause content to be concatenated incorrectly.
/// - Text in document headers and footers is often skipped during extraction.
/// - Standard English section headers (Experience, Education, Skills, Summary) are detected
///   reliably; non-standard labels reduce accuracy.
/// - LinkedIn profile URLs are parsed and surfaced on the candidate record; some Avature
///   deployments enable one-click LinkedIn import via the candidate portal.
/// - Avature's keyword-matching engine scores resumes against job requisition fields, so
///   using keywords directly from the job description is especially impactful.
/// - Date ranges are parsed best in MM/YYYY or Month YYYY formats; purely numeric short
///   formats (e.g. 01/03) are ambiguous and may be misinterpreted.
/// - Images are not parsed; all meaningful content must be selectable text.
/// - Avature supports custom application questions and may ask candidates to confirm or
///   correct fields parsed from the resume — clean parsing reduces friction here.
/// </summary>
public sealed class AvatureProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Avature;
    public override string DisplayName => "Avature";

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
                "Avature's parser reads documents linearly, so multi-column layouts often merge content from separate columns or produce incorrect field order. Use a single-column layout to ensure fields are mapped correctly."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Tables are a common source of parsing errors in Avature: cells are often concatenated in the wrong order or attributed to the wrong field. Replace table-based layouts with plain bullet-point lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Formatting",
                "Images or graphics detected.",
                "Avature does not perform OCR on embedded images. Any content inside images — skill graphics, infographic sections, profile photo labels — is completely invisible to the parser. Replace all image-based content with plain text."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Content detected in document headers or footers.",
                "Avature frequently skips document headers and footers during extraction. Move your name and contact details into the main body of the document to ensure they are captured."));

        // ── Contact information ───────────────────────────────────────────────

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Avature auto-populates candidate record fields from the resume. Missing contact details mean the recruiter must fill them in manually, and your record may be incomplete in the talent pipeline."));

        if (resume.Contact.LinkedIn is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "LinkedIn URL not detected.",
                "Many Avature deployments parse LinkedIn profile URLs and link them to the candidate record. Some portals also offer one-click LinkedIn import. Include your full URL (linkedin.com/in/yourname) in plain text."));

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Add a clearly labelled '{missing}' section using a standard English header. Avature reliably recognises 'Experience', 'Education', and 'Skills'; non-standard or translated labels reduce automated field mapping accuracy."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "No summary or objective section detected.",
                "Avature's keyword-scoring engine weighs resume content against job requisition fields. A professional summary near the top concentrates relevant keywords in a high-visibility area, improving your match score."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Avature extracts employment and education timelines to build your candidate profile. Use unambiguous formats such as 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY' to ensure your career timeline is parsed correctly."));

        // ── Keyword matching advisory ─────────────────────────────────────────

        issues.Add(new ScanIssue(
            IssueSeverity.Info, "Content",
            "Tip: mirror keywords from the job description.",
            "Avature scores resumes against job requisition fields using keyword matching. Using the exact terms from the job posting — especially in a dedicated Skills section — directly increases your automated match score."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
