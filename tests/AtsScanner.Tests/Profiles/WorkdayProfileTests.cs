using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using FluentAssertions;

namespace AtsScanner.Tests.Profiles;

public class WorkdayProfileTests
{
    private readonly WorkdayProfile _profile = new();

    [Fact]
    public void Analyze_CleanResume_ReturnsHighScore()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true);

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
    public void Analyze_TableDetected_RaisesWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.HasTables, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Message.Contains("Table"));
    }

    [Fact]
    public void Analyze_MissingExperience_RaisesStructureWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: false);

        var result = _profile.Analyze(resume);

        result.MissingSections.Should().Contain(SectionType.Experience);
        result.Issues.Should().Contain(i => i.Category == "Structure");
    }

    private static ParsedResume BuildResume(
        ResumeFormatFlags flags, bool hasContact, bool includeCoreSections)
    {
        var contact = hasContact
            ? new ContactInfo("Jane Smith", "jane@example.com", "555-1234", null, null, null, null)
            : new ContactInfo(null, null, null, null, null, null, null);

        var sections = includeCoreSections
            ? new List<ResumeSection>
            {
                new("Work Experience", SectionType.Experience, "Software Engineer at Acme", []),
                new("Education", SectionType.Education, "BS Computer Science", []),
                new("Skills", SectionType.Skills, "C#, .NET, SQL", ["C#", ".NET", "SQL"])
            }
            : new List<ResumeSection>();

        return new ParsedResume(
            FileName: "resume.pdf",
            FileFormat: "pdf",
            RawText: "Jane Smith\njane@example.com\n555-1234\n\nWork Experience\nEducation\nSkills\n2020 – 2023",
            Contact: contact,
            Sections: sections,
            Format: flags);
    }
}
