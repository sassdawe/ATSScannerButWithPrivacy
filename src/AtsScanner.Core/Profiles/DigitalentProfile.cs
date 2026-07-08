using AtsScanner.Core.Models;

namespace AtsScanner.Core.Profiles;

/// <summary>
/// digitalent ag (digitalent.ch) is a Swiss Recruitment Process Outsourcer (RPO)
/// headquartered in Baden, Aargau, that provides recruiting-as-a-service for Swiss
/// and European companies (primarily in IT and digital sectors).
///
/// IMPORTANT: digitalent is NOT a traditional ATS with automated resume parsing.
/// All applications are reviewed by human recruiters. Their publicly stated process is:
///   "Wir sind kein Recruiting-Tool, sondern eine externe Recruiting-Abteilung."
///   ("We are not a recruiting tool, but an external recruiting department.")
///
/// How they process applications:
/// - Candidates submit via a web form (UmbracoForms, no format restriction) or email.
/// - Only email and name are collected as structured fields; everything else must be
///   in the attached document.
/// - Recruiters read and copy-paste text directly from the PDF; no AI/ATS parsing.
///   A 2025 blog post noted they discovered white hidden text when "copying text out
///   of a PDF application" — their standard review workflow.
/// - No machine keyword scoring, no automated section detection.
///
/// Practical scoring guidance:
/// - Focus on human readability, not machine-parsability.
/// - PDF with selectable text is critical (recruiter copy-paste workflow).
/// - White or invisible text is actively caught and damages candidate credibility.
/// - Standard Swiss CV conventions (German-language section headers accepted and common).
/// - Contact information must be in the document body; the form only captures email/name.
/// - French and English CVs are also accepted for respective regions/clients.
/// </summary>
public sealed class DigitalentProfile : BaseAtsPlatformProfile
{
    public override AtsPlatform Platform => AtsPlatform.Digitalent;
    public override string DisplayName => "digitalent.ch";

    private static readonly SectionType[] RequiredSections =
        [SectionType.Experience, SectionType.Education, SectionType.Skills];

    public override ScanResult Analyze(ParsedResume resume)
    {
        var issues = new List<ScanIssue>();

        // ── PDF text layer ────────────────────────────────────────────────────
        // digitalent recruiters copy-paste text from PDFs as their review workflow.
        // A scanned/image-only PDF makes text inaccessible and content invisible.

        if (resume.FileFormat == "pdf" && HasFormat(resume, ResumeFormatFlags.HasImages) &&
            string.IsNullOrWhiteSpace(resume.RawText))
            issues.Add(new ScanIssue(
                IssueSeverity.Critical, "Format",
                "PDF appears to contain no selectable text.",
                "digitalent recruiters copy-paste text from your PDF when reviewing it. A scanned or image-only PDF makes your content completely inaccessible. Export as a text-based PDF from a word processor."));

        // ── Hidden/invisible text ─────────────────────────────────────────────
        // digitalent explicitly called out white text tricks in a 2025 blog post.
        // Detecting invisible text is outside the scope of static format flags, but
        // we can surface a proactive advisory for all submissions.

        issues.Add(new ScanIssue(
            IssueSeverity.Info, "Integrity",
            "Reminder: avoid white or hidden text.",
            "digitalent recruiters have publicly stated they discovered white invisible text in a candidate's PDF when copy-pasting content. Such tricks are noticed, damage your credibility, and are unnecessary — digitalent uses human review, not keyword-filtering AI."));

        // ── Format / readability ──────────────────────────────────────────────

        if (HasFormat(resume, ResumeFormatFlags.HasImages))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Images or graphics detected.",
                "Text embedded in images (e.g. skill bars, infographic sections, profile photo overlays) cannot be copied or read reliably. Keep all professional content as selectable text."));

        if (HasFormat(resume, ResumeFormatFlags.HasMultipleColumns))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Formatting",
                "Multi-column layout detected.",
                "Multi-column layouts can make copy-pasting awkward for human reviewers and may produce garbled text when extracted. A clean single-column layout is easier to read and forward to client hiring managers."));

        // ── Contact information ───────────────────────────────────────────────
        // The digitalent web form only captures email and name as structured fields.
        // All other contact details (phone, address, LinkedIn) must be in the document.

        if (!HasContactInfo(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "Email address or phone number not detected.",
                "The digitalent application form only captures your email address. All other contact details — phone number, LinkedIn, location — must be clearly stated in your attached document so recruiters can reach you."));

        if (resume.Contact.LinkedIn is null)
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Content",
                "LinkedIn URL not detected.",
                "digitalent specialises in IT and digital roles. Including your LinkedIn profile URL in the document helps recruiters quickly verify your background and forward your profile to client companies."));

        // ── Structure ─────────────────────────────────────────────────────────

        foreach (var missing in GetMissingSections(resume, RequiredSections))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Structure",
                $"{missing} section not detected.",
                $"Include a clearly labelled '{missing}' section. German labels are also accepted and common for Swiss applications: 'Berufserfahrung' (Experience), 'Ausbildung' (Education), 'Kenntnisse' / 'Fähigkeiten' (Skills)."));

        if (!HasSection(resume, SectionType.Summary) && !HasSection(resume, SectionType.Objective))
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Structure",
                "No professional summary detected.",
                "A short professional summary ('Profil' or 'Über mich') at the top of your CV helps the digitalent recruiter quickly assess your fit for a role before forwarding your application to the client."));

        // ── Dates ─────────────────────────────────────────────────────────────

        if (!HasValidDates(resume))
            issues.Add(new ScanIssue(
                IssueSeverity.Warning, "Content",
                "No clearly formatted dates detected.",
                "Human reviewers and client hiring managers expect consistent date ranges for employment and education entries. Use 'MM/YYYY – MM/YYYY' or 'Month YYYY – Month YYYY'. Swiss German format '01.2020 – 03.2023' is also standard."));

        // ── PDF advisory ──────────────────────────────────────────────────────

        if (resume.FileFormat == "pdf")
            issues.Add(new ScanIssue(
                IssueSeverity.Info, "Format",
                "PDF format detected.",
                "PDF is the preferred format for Swiss job applications and works well for digitalent. Ensure it is a text-based PDF (text is selectable and copy-pasteable), not a scanned image or design-tool export with outlined fonts."));

        return new ScanResult(
            Platform, CalculateScore(issues), issues,
            GetDetectedSections(resume), GetMissingSections(resume, RequiredSections));
    }
}
