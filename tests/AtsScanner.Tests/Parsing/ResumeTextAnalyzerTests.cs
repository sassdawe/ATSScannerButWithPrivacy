using AtsScanner.Core.Models;
using AtsScanner.Core.Parsing;
using FluentAssertions;

namespace AtsScanner.Tests.Parsing;

public class ResumeTextAnalyzerTests
{
    [Fact]
    public void ExtractContactInfo_WithEmailAndPhone_ParsesBoth()
    {
        const string text = """
            Jane Smith
            jane.smith@example.com | (555) 867-5309
            linkedin.com/in/janesmith
            """;

        var contact = ResumeTextAnalyzer.ExtractContactInfo(text);

        contact.Email.Should().Be("jane.smith@example.com");
        contact.Phone.Should().NotBeNullOrEmpty();
        contact.LinkedIn.Should().Contain("janesmith");
    }

    [Fact]
    public void ExtractContactInfo_WithGitHub_DetectsGitHub()
    {
        const string text = "John Dev | john@dev.io | github.com/johndev";

        var contact = ResumeTextAnalyzer.ExtractContactInfo(text);

        contact.GitHub.Should().Contain("johndev");
    }

    [Fact]
    public void ExtractContactInfo_WithNoContact_ReturnsNulls()
    {
        const string text = "WORK EXPERIENCE\nSoftware Engineer at Acme Corp";

        var contact = ResumeTextAnalyzer.ExtractContactInfo(text);

        contact.Email.Should().BeNull();
        contact.Phone.Should().BeNull();
    }

    [Theory]
    [InlineData("EXPERIENCE", SectionType.Experience)]
    [InlineData("Education", SectionType.Education)]
    [InlineData("skills", SectionType.Skills)]
    [InlineData("Professional Summary", SectionType.Summary)]
    [InlineData("Certifications", SectionType.Certifications)]
    [InlineData("PROJECTS", SectionType.Projects)]
    public void ExtractSections_RecognisesStandardHeaders(string header, SectionType expectedType)
    {
        var text = $"{header}\nSome content under the section.";

        var sections = ResumeTextAnalyzer.ExtractSections(text);

        sections.Should().Contain(s => s.Type == expectedType);
    }

    [Fact]
    public void ExtractSections_WithBullets_ParsesBulletPoints()
    {
        const string text = """
            SKILLS
            • C#
            • .NET
            - SQL
            """;

        var sections = ResumeTextAnalyzer.ExtractSections(text);
        var skillsSection = sections.FirstOrDefault(s => s.Type == SectionType.Skills);

        skillsSection.Should().NotBeNull();
        skillsSection!.BulletPoints.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void ExtractSections_WithNoHeaders_ReturnsEmpty()
    {
        const string text = "This is just a paragraph of text with no clear section headers.";

        var sections = ResumeTextAnalyzer.ExtractSections(text);

        sections.Should().BeEmpty();
    }
}
