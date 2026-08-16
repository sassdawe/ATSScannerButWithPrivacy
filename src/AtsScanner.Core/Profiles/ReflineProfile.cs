using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// Refline AG (refline.io) is a Swiss applicant tracking system provider, founded in
/// 2002 and headquartered in Switzerland, used by SMEs, large companies, NGOs, and
/// public-sector organisations for applicant management and recruiting.
///
/// Key characteristics:
/// - Candidates apply through a structured, configurable web application form. The
///   form captures name, email, and phone as discrete fields, but the attached CV
///   document is still parsed to populate the candidate dossier used by recruiters
///   and hiring managers for screening and comparison.
/// - Strongly DACH-region focused: German is the primary language, with French and
///   Italian also common for Swiss postings across cantons and multilingual
///   organisations. Section header recognition favours German labels
///   ("Berufserfahrung", "Ausbildung", "Kenntnisse") alongside English equivalents.
/// - As a configurable, modular ATS aimed at simplified pre-screening, the parser
///   favours simple, linear, single-column documents; multi-column layouts and
///   tables are a common source of misassigned fields in the generated dossier.
/// - Contact information (email, phone) should appear as plain text in the document
///   body in addition to the web form, since recruiters frequently reference the
///   attached document directly rather than the structured form fields.
/// - Employment history dates are used to build the candidate's professional
///   timeline in the dossier; missing or inconsistently formatted dates degrade the
///   automatically generated summary shown to recruiters and line managers.
/// - Image-based/scanned PDFs are not reliably parseable — only text-based PDF and
///   DOCX are reliably supported.
/// </summary>
public sealed class ReflineProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Refline;
    public override string DisplayName => "refline.io";

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
                "Refline parses documents top-to-bottom in a single pass when building the candidate dossier. Side-by-side columns will be merged, scrambling your experience and skills sections. Use a single-column layout."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Table cells are read in unpredictable order by Refline's parser. Replace skills or education tables with plain bullet-point lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Text embedded in images (including profile photos with overlaid text) is not extracted. Remove decorative graphics and keep all content as selectable text."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Document headers or footers detected.",
                "Refline may not extract contact information placed in document headers or footers. Move your name, email, and phone number into the main document body."));

        // ── Contact information ───────────────────────────────────────────────

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Although Refline's application form collects contact details separately, recruiters often reference your attached document directly. Include plain-text email and phone at the top of the document body."));

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Use a clearly labelled section header such as 'Work Experience' / 'Berufserfahrung' (Experience), 'Education' / 'Ausbildung', or 'Skills' / 'Kenntnisse'. Refline recognises both English and German headers."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Structure",
                "No summary or objective section detected.",
                "A short professional summary ('Profil' or 'Zusammenfassung') helps recruiters and line managers quickly assess fit when reviewing the candidate dossier in Refline."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Refline builds a chronological employment timeline from date ranges in the candidate dossier. Use 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY' consistently. Swiss German formats such as '01.2020 – 03.2023' are also recognised."));

        // ── PDF-specific ──────────────────────────────────────────────────────

        if (resume.FileFormat == "pdf")
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Format",
                "PDF format detected.",
                "Refline accepts PDF, but a text-based PDF created from a word processor parses more reliably than one exported from a design tool. Ensure the text is selectable (not a scanned image)."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
