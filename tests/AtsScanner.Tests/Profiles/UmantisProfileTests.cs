using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using FluentAssertions;

namespace AtsScanner.Tests.Profiles;

public class UmantisProfileTests
{
    private readonly UmantisProfile _profile = new();

    [Fact]
    public void Analyze_CleanResume_ReturnsHighScore()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true, fileFormat: "docx");

        var result = _profile.Analyze(resume);

        result.Score.Should().BeGreaterThanOrEqualTo(80);
        result.Issues.Should().NotContain(i => i.Severity == IssueSeverity.Critical);
    }

    [Fact]
    public void Analyze_MultiColumnLayout_RaisesCriticalIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.HasMultipleColumns, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Critical &&
            i.Category == "Formatting");
        result.Score.Should().BeLessThan(85);
    }

    [Fact]
    public void Analyze_TablesDetected_RaisesWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.HasTables, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Message.Contains("Table"));
    }

    [Fact]
    public void Analyze_MissingContactInfo_RaisesWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: false, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Content" &&
            i.Message.Contains("Email"));
    }

    [Fact]
    public void Analyze_MissingExperienceSection_RaisesStructureWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: false);

        var result = _profile.Analyze(resume);

        result.MissingSections.Should().Contain(SectionType.Experience);
        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Structure");
    }

    [Fact]
    public void Analyze_NoDates_RaisesDateWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true, includeDates: false);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Message.Contains("date"));
    }

    [Fact]
    public void Analyze_PdfFormat_RaisesInfoIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true, fileFormat: "pdf");

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Info &&
            i.Category == "Format");
    }

    [Fact]
    public void Analyze_ScoreAlwaysInValidRange()
    {
        var worstCase = BuildResume(
            ResumeFormatFlags.HasMultipleColumns | ResumeFormatFlags.HasTables |
            ResumeFormatFlags.HasImages | ResumeFormatFlags.HasHeaders | ResumeFormatFlags.HasFooters,
            hasContact: false, includeCoreSections: false, includeDates: false);

        var result = _profile.Analyze(worstCase);

        result.Score.Should().BeInRange(0, 100);
    }

    private static ParsedResume BuildResume(
        ResumeFormatFlags flags,
        bool hasContact,
        bool includeCoreSections,
        bool includeDates = true,
        string fileFormat = "pdf")
    {
        var contact = hasContact
            ? new ContactInfo("Anna Müller", "anna.mueller@example.com", "+49 89 123456", null, "linkedin.com/in/annamueller", null, null)
            : new ContactInfo(null, null, null, null, null, null, null);

        var sections = includeCoreSections
            ? new List<ResumeSection>
            {
                new("Work Experience", SectionType.Experience, "Software Engineer at Beispiel GmbH", []),
                new("Education", SectionType.Education, "M.Sc. Informatik, TU München", []),
                new("Skills", SectionType.Skills, "C#, .NET, SQL, Azure", ["C#", ".NET", "SQL"])
            }
            : new List<ResumeSection>();

        var rawText = string.Join("\n",
        [
            hasContact ? "Anna Müller\nanna.mueller@example.com\n+49 89 123456" : "",
            includeCoreSections ? "Work Experience\nEducation\nSkills" : "",
            includeDates ? "01/2020 – 03/2024" : ""
        ]);

        return new ParsedResume(
            FileName: $"lebenslauf.{fileFormat}",
            FileFormat: fileFormat,
            RawText: rawText,
            Contact: contact,
            Sections: sections,
            Format: flags);
    }
}
