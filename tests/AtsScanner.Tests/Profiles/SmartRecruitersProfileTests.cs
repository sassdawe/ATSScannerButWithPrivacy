using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using FluentAssertions;

namespace AtsScanner.Tests.Profiles;

public class SmartRecruitersProfileTests
{
    private readonly SmartRecruitersProfile _profile = new();

    [Fact]
    public void Analyze_CleanResume_ReturnsHighScore()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Score.Should().BeGreaterThanOrEqualTo(75);
        result.Issues.Should().NotContain(i => i.Severity == IssueSeverity.Critical);
    }

    [Fact]
    public void Analyze_MultiColumnLayout_RaisesFormattingWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.HasMultipleColumns, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Formatting" &&
            i.Message.Contains("Multi-column"));
    }

    [Fact]
    public void Analyze_TablesDetected_RaisesFormattingWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.HasTables, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Formatting" &&
            i.Message.Contains("Table"));
    }

    [Fact]
    public void Analyze_ImagesDetected_RaisesFormattingWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.HasImages, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Formatting" &&
            i.Message.Contains("Images"));
    }

    [Fact]
    public void Analyze_ContentInHeaderOrFooter_RaisesFormattingWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.HasHeaders, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Formatting" &&
            i.Message.Contains("headers or footers"));
    }

    [Fact]
    public void Analyze_MissingContactInfo_RaisesContentWarning()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: false, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Warning &&
            i.Category == "Content" &&
            i.Message.Contains("Email"));
    }

    [Fact]
    public void Analyze_MissingLinkedIn_RaisesInfoIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true, includeLinkedIn: false);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Info &&
            i.Category == "Content" &&
            i.Message.Contains("LinkedIn"));
    }

    [Fact]
    public void Analyze_MissingGitHub_RaisesInfoIssue()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true, includeGitHub: false);

        var result = _profile.Analyze(resume);

        result.Issues.Should().Contain(i =>
            i.Severity == IssueSeverity.Info &&
            i.Category == "Content" &&
            i.Message.Contains("GitHub"));
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
            i.Category == "Content" &&
            i.Message.Contains("date"));
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

    [Fact]
    public void Analyze_PlatformIsSmartRecruiters()
    {
        var resume = BuildResume(ResumeFormatFlags.None, hasContact: true, includeCoreSections: true);

        var result = _profile.Analyze(resume);

        result.Platform.Should().Be(AtsPlatform.SmartRecruiters);
    }

    private static ParsedResume BuildResume(
        ResumeFormatFlags flags,
        bool hasContact,
        bool includeCoreSections,
        bool includeDates = true,
        bool includeLinkedIn = true,
        bool includeGitHub = true)
    {
        var contact = hasContact
            ? new ContactInfo(
                "Jane Smith",
                "jane.smith@example.com",
                "+1 555 123 4567",
                null,
                includeLinkedIn ? "linkedin.com/in/janesmith" : null,
                includeGitHub ? "github.com/janesmith" : null,
                null)
            : new ContactInfo(null, null, null, null, null, null, null);

        var sections = includeCoreSections
            ? new List<ResumeSection>
            {
                new("Experience", SectionType.Experience, "Software Engineer at Example Corp", []),
                new("Education", SectionType.Education, "B.Sc. Computer Science, MIT", []),
                new("Skills", SectionType.Skills, "C#, .NET, Azure, SQL", ["C#", ".NET", "Azure"])
            }
            : new List<ResumeSection>();

        var rawText = string.Join("\n",
        [
            hasContact ? "Jane Smith\njane.smith@example.com\n+1 555 123 4567" : "",
            includeCoreSections ? "Experience\nEducation\nSkills" : "",
            includeDates ? "01/2020 – 03/2024" : ""
        ]);

        return new ParsedResume(
            FileName: "resume.pdf",
            FileFormat: "pdf",
            RawText: rawText,
            Contact: contact,
            Sections: sections,
            Format: flags);
    }
}
