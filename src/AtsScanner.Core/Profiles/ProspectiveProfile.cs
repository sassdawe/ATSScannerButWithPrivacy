using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// prospective AG (prospective.ch) is a Swiss e-recruiting and talent management
/// platform based in Zurich, widely used by Swiss public administrations
/// (cantons, cities, municipalities) as well as private-sector SMEs.
///
/// Key characteristics:
/// - Candidates apply through a structured web application form; the form itself
///   captures name, email, and phone as discrete fields, but the attached CV
///   document is still parsed to populate the candidate profile and searchable
///   talent pool.
/// - Strongly DACH-region focused: German is the primary language, with French
///   and Italian also common for Swiss cantonal/federal postings. Section header
///   recognition favours German labels ("Berufserfahrung", "Ausbildung",
///   "Kenntnisse") alongside English equivalents.
/// - The parser favours simple, linear, single-column documents; multi-column
///   layouts and tables are a common source of misassigned fields, especially
///   for public-sector postings that receive high application volumes and rely
///   on automated pre-screening.
/// - Contact information (email, phone) must appear as plain text in the
///   document body in addition to the web form, since some workflows re-extract
///   it for internal HR system synchronisation.
/// - Employment history dates are used to build a chronological timeline in the
///   candidate profile; missing or inconsistently formatted dates degrade the
///   automatically generated CV summary shown to recruiters.
/// - Image-based/scanned PDFs are not parseable — only text-based PDF and DOCX
///   are reliably supported.
/// </summary>
public sealed class ProspectiveProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Prospective;
    public override string DisplayName => "prospective.ch";

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
                "prospective parses documents top-to-bottom in a single pass. Side-by-side columns will be merged, scrambling your experience and skills sections. Use a single-column layout."));

        if (HasFormat(resume, ResumeFormatFlags.HasTables))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Tables detected.",
                "Table cells are read in unpredictable order by prospective. Replace skills or education tables with plain bullet-point lists."));

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Text embedded in images (including profile photos with overlaid text) is not extracted. Remove decorative graphics and keep all content as selectable text."));

        if (HasFormat(resume, ResumeFormatFlags.HasHeaders) || HasFormat(resume, ResumeFormatFlags.HasFooters))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Document headers or footers detected.",
                "prospective may not extract contact information placed in document headers or footers. Move your name, email, and phone number into the main document body."));

        // ── Contact information ───────────────────────────────────────────────

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "Although prospective's application form collects contact details separately, the candidate profile also re-extracts them from your document. Include plain-text email and phone at the top of the document body."));

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Use a clearly labelled section header such as 'Work Experience' / 'Berufserfahrung' (Experience), 'Education' / 'Ausbildung', or 'Skills' / 'Kenntnisse'. prospective recognises both English and German headers."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Structure",
                "No summary or objective section detected.",
                "A short professional summary ('Profil' or 'Zusammenfassung') is displayed prominently in the prospective candidate profile shown to recruiters and helps them quickly assess fit."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "prospective builds a chronological employment timeline from date ranges. Use 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY' consistently. Swiss German formats such as '01.2020 – 03.2023' are also recognised."));

        // ── PDF-specific ──────────────────────────────────────────────────────

        if (resume.FileFormat == "pdf")
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Format",
                "PDF format detected.",
                "prospective accepts PDF, but a text-based PDF created from a word processor parses more reliably than one exported from a design tool. Ensure the text is selectable (not a scanned image)."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
