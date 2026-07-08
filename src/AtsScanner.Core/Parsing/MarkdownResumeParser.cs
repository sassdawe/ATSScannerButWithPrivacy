using System.Text;
using System.Text.RegularExpressions;
using AtsScanner.Core.Models;

namespace AtsScanner.Core.Parsing;

public sealed class MarkdownResumeParser : IResumeParser
{
    private static readonly Regex HeadingRegex = new(@"^\s{0,3}#{1,6}\s+", RegexOptions.Compiled);
    private static readonly Regex BlockQuoteRegex = new(@"^\s*>\s?", RegexOptions.Compiled);
    private static readonly Regex TableSeparatorRegex = new(@"^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$", RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^\)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);
    private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\(([^\)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex EmphasisRegex = new(@"(\*\*|__)(.*?)\1|(\*|_)(.*?)\3", RegexOptions.Compiled);

    public IReadOnlyList<string> SupportedExtensions { get; } = [".md", ".markdown"];

    public async Task<ParsedResume> ParseAsync(string filePath, CancellationToken ct = default)
    {
        var markdown = await File.ReadAllTextAsync(filePath, ct);

        var (rawText, format) = NormalizeMarkdown(markdown);
        var contact = ResumeTextAnalyzer.ExtractContactInfo(rawText);
        var sections = ResumeTextAnalyzer.ExtractSections(rawText);

        return new ParsedResume(
            FileName: Path.GetFileName(filePath),
            FileFormat: "md",
            RawText: rawText,
            Contact: contact,
            Sections: sections,
            Format: format
        );
    }

    private static (string RawText, ResumeFormatFlags Format) NormalizeMarkdown(string markdown)
    {
        var lines = markdown.Split('\n');
        var output = new StringBuilder();
        var inCodeFence = false;
        var format = ResumeFormatFlags.None;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');

            if (line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("~~~"))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
                continue;

            if (line.Contains("![", StringComparison.Ordinal) && line.Contains("](", StringComparison.Ordinal))
                format |= ResumeFormatFlags.HasImages;

            if (line.Contains('|') && !line.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
                format |= ResumeFormatFlags.HasTables;

            if (TableSeparatorRegex.IsMatch(line))
                continue;

            var cleaned = line;
            cleaned = HeadingRegex.Replace(cleaned, string.Empty);
            cleaned = BlockQuoteRegex.Replace(cleaned, string.Empty);
            cleaned = ImageRegex.Replace(cleaned, "$1 $2");
            cleaned = LinkRegex.Replace(cleaned, "$1 $2");
            cleaned = InlineCodeRegex.Replace(cleaned, "$1");
            cleaned = EmphasisRegex.Replace(cleaned, m => m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value);

            output.AppendLine(cleaned);
        }

        return (output.ToString(), format);
    }
}