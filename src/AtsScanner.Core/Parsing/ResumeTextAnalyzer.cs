using AtsScanner.Core.Models;

namespace AtsScanner.Core.Parsing;

/// <summary>
/// Shared utilities for extracting structured data from raw resume text.
/// </summary>
public static class ResumeTextAnalyzer
{
    private static readonly Dictionary<string, SectionType> SectionKeywords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["summary"] = SectionType.Summary,
            ["professional summary"] = SectionType.Summary,
            ["profile"] = SectionType.Summary,
            ["professional profile"] = SectionType.Summary,
            ["about me"] = SectionType.Summary,
            ["objective"] = SectionType.Objective,
            ["career objective"] = SectionType.Objective,
            ["experience"] = SectionType.Experience,
            ["work experience"] = SectionType.Experience,
            ["work history"] = SectionType.Experience,
            ["employment history"] = SectionType.Experience,
            ["employment"] = SectionType.Experience,
            ["career history"] = SectionType.Experience,
            ["professional experience"] = SectionType.Experience,
            ["relevant experience"] = SectionType.Experience,
            ["education"] = SectionType.Education,
            ["academic background"] = SectionType.Education,
            ["educational background"] = SectionType.Education,
            ["academic history"] = SectionType.Education,
            ["skills"] = SectionType.Skills,
            ["technical skills"] = SectionType.Skills,
            ["core competencies"] = SectionType.Skills,
            ["competencies"] = SectionType.Skills,
            ["key skills"] = SectionType.Skills,
            ["areas of expertise"] = SectionType.Skills,
            ["certifications"] = SectionType.Certifications,
            ["certification"] = SectionType.Certifications,
            ["licenses"] = SectionType.Certifications,
            ["licenses & certifications"] = SectionType.Certifications,
            ["projects"] = SectionType.Projects,
            ["project experience"] = SectionType.Projects,
            ["key projects"] = SectionType.Projects,
            ["awards"] = SectionType.Awards,
            ["achievements"] = SectionType.Awards,
            ["honors"] = SectionType.Awards,
            ["honors & awards"] = SectionType.Awards,
            ["publications"] = SectionType.Publications,
            ["volunteer"] = SectionType.Volunteer,
            ["volunteer experience"] = SectionType.Volunteer,
            ["community service"] = SectionType.Volunteer,
            ["languages"] = SectionType.Languages,
            ["references"] = SectionType.References,
        };

    private static readonly System.Text.RegularExpressions.Regex EmailRegex =
        new(@"[\w.+-]+@[\w-]+\.[a-zA-Z]{2,}", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex PhoneRegex =
        new(@"[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex LinkedInRegex =
        new(@"linkedin\.com/in/[\w-]+", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex GitHubRegex =
        new(@"github\.com/[\w-]+", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public static ContactInfo ExtractContactInfo(string rawText)
    {
        var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var email = EmailRegex.Match(rawText).Value.NullIfEmpty();
        var phone = PhoneRegex.Match(rawText).Value.NullIfEmpty();
        var linkedin = LinkedInRegex.Match(rawText).Value.NullIfEmpty();
        var github = GitHubRegex.Match(rawText).Value.NullIfEmpty();

        // Heuristic: name is the first non-empty line that isn't an email/phone/URL
        var name = lines.FirstOrDefault(l =>
            !EmailRegex.IsMatch(l) &&
            !PhoneRegex.IsMatch(l) &&
            !l.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            l.Length is > 3 and < 60);

        return new ContactInfo(name, email, phone, Location: null, linkedin, github, Website: null);
    }

    public static IReadOnlyList<ResumeSection> ExtractSections(string rawText)
    {
        var lines = rawText.Split('\n', StringSplitOptions.TrimEntries);
        var sections = new List<ResumeSection>();
        string? currentTitle = null;
        SectionType currentType = SectionType.Unknown;
        var currentLines = new List<string>();

        foreach (var line in lines)
        {
            if (IsSectionHeader(line, out var detectedType))
            {
                if (currentTitle is not null)
                    sections.Add(BuildSection(currentTitle, currentType, currentLines));

                currentTitle = line;
                currentType = detectedType;
                currentLines = [];
            }
            else if (currentTitle is not null)
            {
                currentLines.Add(line);
            }
        }

        if (currentTitle is not null)
            sections.Add(BuildSection(currentTitle, currentType, currentLines));

        return sections;
    }

    private static readonly char[] BulletChars = ['•', '·', '▪', '▶', '◆', '–', '—'];

    private static bool IsSectionHeader(string line, out SectionType type)
    {
        type = SectionType.Unknown;

        // Lines starting with bullet markers or digits+dot (list items) are never headers.
        if (line.Length > 0 && (BulletChars.Contains(line[0]) ||
            line.StartsWith("- ") || line.StartsWith("* ") ||
            (char.IsDigit(line[0]) && line.Length > 1 && line[1] == '.')))
            return false;

        var trimmed = line.Trim(':', ' ', '_').Trim();

        if (trimmed.Length is 0 or > 50)
            return false;

        // Exact match against known keywords
        if (SectionKeywords.TryGetValue(trimmed, out type))
            return true;

        // All-caps line that's not too long — likely a header
        if (trimmed == trimmed.ToUpperInvariant() && trimmed.Length > 2)
        {
            if (SectionKeywords.TryGetValue(trimmed.ToLowerInvariant(), out type))
                return true;
            type = SectionType.Unknown;
            return true; // Still treat as section header
        }

        return false;
    }

    private static ResumeSection BuildSection(string title, SectionType type, List<string> lines)
    {
        var bullets = lines
            .Where(l => l.StartsWith("•") || l.StartsWith("-") || l.StartsWith("*") || l.StartsWith("·"))
            .Select(l => l.TrimStart('•', '-', '*', '·', ' '))
            .Where(l => l.Length > 0)
            .ToList();

        return new ResumeSection(title, type, string.Join("\n", lines), bullets);
    }

    private static string? NullIfEmpty(this string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
