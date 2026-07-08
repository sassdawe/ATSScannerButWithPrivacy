using AtsScanner.Core.Models;
using AtsScanner.Core.Parsing;
using FluentAssertions;

namespace AtsScanner.Tests.Parsing;

public sealed class MarkdownResumeParserTests
{
    [Theory]
    [InlineData("resume.md")]
    [InlineData("resume.markdown")]
    public void ResumeParserFactory_WithMarkdownExtension_ReturnsMarkdownParser(string fileName)
    {
        var parser = ResumeParserFactory.GetParser(fileName);

        parser.Should().BeOfType<MarkdownResumeParser>();
    }

    [Fact]
    public async Task ParseAsync_WithMarkdownResume_ExtractsContactAndSections()
    {
        const string markdown = """
            # Jane Dev

            jane.dev@example.com
            +1 (555) 123-4567
            [LinkedIn](https://linkedin.com/in/janedev)
            [GitHub](https://github.com/janedev)

            ## EXPERIENCE
            - Built C# services
            - Improved ATS parsing quality

            ## SKILLS
            - C#
            - .NET
            """;

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(tempPath, markdown);

        try
        {
            var parser = new MarkdownResumeParser();

            var parsed = await parser.ParseAsync(tempPath);

            parsed.FileFormat.Should().Be("md");
            parsed.Contact.Email.Should().Be("jane.dev@example.com");
            parsed.Contact.LinkedIn.Should().Contain("linkedin.com/in/janedev");
            parsed.Contact.GitHub.Should().Contain("github.com/janedev");

            parsed.Sections.Should().Contain(s => s.Type == SectionType.Experience);
            parsed.Sections.Should().Contain(s => s.Type == SectionType.Skills);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}