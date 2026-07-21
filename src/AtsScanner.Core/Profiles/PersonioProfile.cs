using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Personio is an all-in-one HR and recruiting platform popular with small and
/// mid-sized companies across the DACH region (Germany, Austria, Switzerland)
/// and increasingly across the rest of Europe.
///
/// Key parsing characteristics:
/// - Accepts PDF and DOCX; the built-in resume parser auto-fills candidate
///   profile fields (name, email, phone, experience, education) from the
///   uploaded document, so clean text extraction directly affects data quality.
/// - Bilingual by design: both German and English section headers are
///   recognised (e.g. "Berufserfahrung"/"Experience", "Ausbildung"/"Education",
///   "Fähigkeiten"/"Skills"), but mixed-language documents can confuse field
///   auto-detection.
/// - Single-column, linear layouts parse most reliably; multi-column resumes
///   and side-by-side date/description layouts often get merged out of order.
/// - Tables are not well supported — cell content can be dropped or
///   concatenated incorrectly, especially in skills matrices.
/// - Text inside headers/footers is frequently missed by the auto-fill parser,
///   so contact details should always sit in the main document body.
/// - Personio surfaces a candidate's LinkedIn/XING profile link if detected in
///   plain text, which recruiters commonly rely on for quick verification.
/// - Structured date ranges are used to auto-populate the employment and
///   education timeline; both "MM/YYYY – MM/YYYY" and German "MM.YYYY – MM.YYYY"
///   formats are recognised.
/// - Since many Personio customers are smaller companies without dedicated
///   recruiters, resumes are often reviewed manually after auto-fill, so
///   readability of the source document still matters even where parsing
///   partially fails.
/// - Images and scanned/flattened PDF pages are not OCR'd; all content must be
///   selectable text.
/// </summary>
public sealed class PersonioProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Personio;
    public override string DisplayName => "Personio";

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
                "Personio's auto-fill parser reads documents linearly and often merges content from side-by-side columns. Use a single-column layout so experience and skills are captured correctly."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Table cells are frequently dropped or concatenated out of order by Personio's parser. Replace skills or education tables with plain bullet-point lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Personio does not perform OCR on embedded images or scanned pages. Any content inside images is invisible to the auto-fill parser — keep all content as selectable text."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Document headers or footers detected.",
                "Contact details placed in document headers or footers are often missed by Personio's auto-fill. Move your name, email, and phone number into the main document body."));

        // ── Contact information ───────────────────────────────────────────────

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Personio auto-fills the candidate profile from contact details in the document body. Include a plain-text email and phone number near the top of the resume."));

        if (resume.Contact.LinkedIn is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "LinkedIn URL not detected.",
                "Personio surfaces a candidate's LinkedIn (or XING) profile link when present in plain text, which recruiters use for quick verification. Include your full profile URL."));

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Use a clearly labelled section header such as 'Work Experience' / 'Berufserfahrung' (Experience), 'Education' / 'Ausbildung', or 'Skills' / 'Fähigkeiten'. Personio recognises both English and German headers."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Structure",
                "No summary or objective section detected.",
                "A short professional summary helps human reviewers quickly assess fit, which matters at Personio's typically smaller customers where resumes are often reviewed manually after auto-fill."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Personio builds the employment and education timeline from date ranges. Use 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY' consistently. German formats such as '01.2020 – 03.2023' are also recognised."));

        // ── PDF-specific ──────────────────────────────────────────────────────

        if (resume.FileFormat == "pdf")
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Format",
                "PDF format detected.",
                "Personio accepts PDF, but ensure the text is selectable rather than a scanned image — scanned pages are not OCR'd and will be treated as empty by the auto-fill parser."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
