using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using FluentAssertions;

namespace AtsScanner.Tests.Profiles;

public class TaleoProfileTests
{
    private readonly TaleoProfile _profile = new();

    [Fact]
    public void Analyze_MultiColumnLayout_RaisesCriticalIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.HasMultipleColumns);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i => i.Severity == IssueSeverity.Critical);
        result.Score.Should().BeLessThan(85);
    }

    [Fact]
    public void Analyze_TablesDetected_RaisesCriticalIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.HasTables);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Critical &&
            i.Message.Contains("Table"));
    }

    [Fact]
    public void Analyze_PdfFormat_RaisesInfoIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.None); // fileFormat = "pdf" by default

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Info &&
            i.Category == "Format");
    }

    [Fact]
    public void Analyze_PerfectDocx_ScoresHighest()
    {
        var resume = BuildResume(ResumeFormatFlags.None, fileFormat: "docx");

        var result = _profile.Analyze(resume);

        result.Issues.Should().NotContain(i => i.Severity == IssueSeverity.Critical);
        result.Score.Should().BeGreaterThanOrEqualTo(75);
    }

    private static ParsedResume BuildResume(ResumeFormatFlags flags, string fileFormat = "pdf")
    {
        var contact = new ContactInfo("Jane Smith", "jane@example.com", "555-1234", null, null, null, null);
        var sections = new List<ResumeSection>
        {
            new("Work Experience", SectionType.Experience, "Software Engineer", []),
            new("Education", SectionType.Education, "BS CS", []),
            new("Skills", SectionType.Skills, "C#, SQL", [])
        };

        return new ParsedResume(
            FileName: $"resume.{fileFormat}",
            FileFormat: fileFormat,
            RawText: "Jane Smith\njane@example.com\nWork Experience\nEducation\nSkills\nJan 2020 - Mar 2023",
            Contact: contact,
            Sections: sections,
            Format: flags);
    }
}
