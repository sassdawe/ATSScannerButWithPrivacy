using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Haufe-umantis (now Haufe Talent Management) is a Swiss-origin ATS widely
/// used in DACH-region companies (Germany, Austria, Switzerland).
///
/// Key characteristics:
/// - Web-based upload form; both PDF and DOCX are accepted, but the internal
///   parser is conservative and prefers simple, linear document structures.
/// - Multilingual: German, French, Italian, and English are all supported,
///   but section header recognition is tuned to German-language labels in
///   many deployments (e.g. "Berufserfahrung", "Ausbildung", "Kenntnisse").
///   English standard headers are also recognised.
/// - Special characters and diacritics (ä, ö, ü, ß, é, etc.) are handled
///   correctly — no need to avoid them.
/// - Tables and multi-column layouts cause the same parsing problems as with
///   Workday: content is merged linearly and fields are misassigned.
/// - Contact fields (email, phone, address) are auto-extracted and mapped to
///   structured candidate profile fields; they must be in the document body.
/// - The platform relies on structured date ranges to build the employment
///   history timeline; malformed or missing dates result in incomplete profiles.
/// - LinkedIn URLs are recognised and stored as a profile link.
/// - PDF files with image-based text (scanned documents) are not parseable.
/// </summary>
public sealed class UmantisProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Umantis;
    public override string DisplayName => "Haufe-umantis";

    private static readonly SectionType[] RequiredSections =
        [SectionType.Experience, SectionType.Education, SectionType.Skills];

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        // ── Formatting ────────────────────────────────────────────────────────

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Formatting",
                "Multi-column layout detected.",
                "Haufe-umantis parses documents top-to-bottom in a single pass. Side-by-side columns will be merged, scrambling your experience and skills sections. Use a single-column layout."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Table cells are read in unpredictable order by umantis. Replace skills or education tables with plain bullet-point lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Text embedded in images (including profile photos with overlaid text) is not extracted. Remove decorative graphics and keep all content as selectable text."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Document headers or footers detected.",
                "Haufe-umantis may not extract contact information placed in document headers or footers. Move your name, email, and phone number into the main document body."));

        // ── Contact information ───────────────────────────────────────────────

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Haufe-umantis auto-maps contact fields to the candidate profile. Include plain-text email and phone at the top of the document body."));

        if (resume.Contact.LinkedIn is not null)
        {
            // LinkedIn is explicitly supported — no issue, but worth calling out as a positive
            // (handled in the verbose positive checklist via the shared contact detection)
        }

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Use a clearly labelled section header such as 'Work Experience' / 'Berufserfahrung' (Experience), 'Education' / 'Ausbildung', or 'Skills' / 'Kenntnisse'. umantis recognises both English and German headers."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Structure",
                "No summary or objective section detected.",
                "A short professional summary ('Profil' or 'Zusammenfassung') is displayed prominently in the umantis candidate profile and helps recruiters quickly assess fit."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Haufe-umantis builds an employment timeline from date ranges. Use 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY' consistently. German formats such as '01.2020 – 03.2023' are also recognised."));

        // ── PDF-specific ──────────────────────────────────────────────────────

        if (resume.FileFormat == "pdf")
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Format",
                "PDF format detected.",
                "umantis accepts PDF, but a text-based PDF created from a word processor parses more reliably than one exported from a design tool. Ensure the text is selectable (not a scanned image)."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
