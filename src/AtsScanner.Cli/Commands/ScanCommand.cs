using AtsScanner.Core.Analysis;
using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace AtsScanner.Cli.Commands;

public sealed class ScanCommand : AsyncCommand<ScanSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, ScanSettings settings, CancellationToken ct)
    {
        var filePath = Path.GetFullPath(settings.ResumePath);

        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File not found: {filePath}");
            return 1;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is not ".pdf" and not ".docx")
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Only .pdf and .docx files are supported.");
            return 1;
        }

        IReadOnlyList<ScanResult> results = [];
        ParsedResume? parsedResume = null;
        var analyzer = new ResumeAnalyzer();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Scanning [blue]{Path.GetFileName(filePath)}[/]...", async _ =>
            {
                parsedResume = await analyzer.ParseAsync(filePath, ct);

                if (settings.Platform.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    results = await analyzer.AnalyzeAllAsync(filePath, ct);
                }
                else if (ProfileRegistry.TryParse(settings.Platform, out var platform))
                {
                    results = [await analyzer.AnalyzeAsync(filePath, platform, ct)];
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Unknown platform '{settings.Platform}'.");
                }
            });

        if (!results.Any() || parsedResume is null) return 1;

        if (settings.Output.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.WriteLine(json);
            return 0;
        }

        if (settings.Verbose)
            RenderVerbose(Path.GetFileName(filePath), parsedResume, results);
        else
            RenderResults(Path.GetFileName(filePath), results);

        return 0;
    }

    // ── Standard output ──────────────────────────────────────────────────────

    private static void RenderResults(string fileName, IReadOnlyList<ScanResult> results)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("ATS Scanner").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[grey]Resume:[/] [white]{fileName}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(BuildSummaryTable(results));

        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            if (!result.Issues.Any()) continue;

            var profileName = ProfileRegistry.Get(result.Platform).DisplayName;
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[blue]{profileName}[/] — Details").RuleStyle("blue dim"));

            foreach (var issue in result.Issues.OrderBy(i => i.Severity))
            {
                var (icon, color) = IssueStyle(issue.Severity);
                AnsiConsole.MarkupLine($"  [{color}]{icon} [{issue.Category}][/] {Markup.Escape(issue.Message)}");
                if (issue.Suggestion is not null)
                    AnsiConsole.MarkupLine($"    [grey]→ {Markup.Escape(issue.Suggestion)}[/]");
            }

            RenderSectionLists(result);
        }

        AnsiConsole.WriteLine();
    }

    // ── Verbose output ────────────────────────────────────────────────────────

    private static void RenderVerbose(string fileName, ParsedResume resume, IReadOnlyList<ScanResult> results)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("ATS Scanner").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[grey]Resume:[/] [white]{fileName}[/]  [grey]({resume.FileFormat.ToUpperInvariant()})[/]");
        AnsiConsole.WriteLine();

        RenderDocumentAnalysis(resume);
        AnsiConsole.WriteLine();

        AnsiConsole.Write(BuildSummaryTable(results));

        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            var profileName = ProfileRegistry.Get(result.Platform).DisplayName;
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[blue]{profileName}[/]  {ScoreBadge(result)}").RuleStyle("blue dim"));

            RenderPositiveChecklist(resume, result);

            if (result.Issues.Any())
            {
                AnsiConsole.WriteLine();
                RenderIssuesByCategory(result);
            }

            RenderSectionLists(result);
        }

        RenderPriorityActions(results);
        AnsiConsole.WriteLine();
    }

    // ── Verbose sub-sections ──────────────────────────────────────────────────

    private static void RenderDocumentAnalysis(ParsedResume resume)
    {
        AnsiConsole.Write(new Rule("[blue]Document Analysis[/]").RuleStyle("blue dim"));

        // Contact info
        var contact = resume.Contact;
        AnsiConsole.MarkupLine("  [grey]Contact info[/]");
        AnsiConsole.MarkupLine(contact.Email is not null
            ? $"    [green]✔[/] Email:    {Markup.Escape(contact.Email)}"
            : "    [red]✖[/] Email:    not detected");
        AnsiConsole.MarkupLine(contact.Phone is not null
            ? $"    [green]✔[/] Phone:    {Markup.Escape(contact.Phone)}"
            : "    [red]✖[/] Phone:    not detected");
        AnsiConsole.MarkupLine(contact.LinkedIn is not null
            ? $"    [green]✔[/] LinkedIn: {Markup.Escape(contact.LinkedIn)}"
            : "    [grey]–[/]  LinkedIn: not detected");
        AnsiConsole.MarkupLine(contact.GitHub is not null
            ? $"    [green]✔[/] GitHub:   {Markup.Escape(contact.GitHub)}"
            : "    [grey]–[/]  GitHub:   not detected");

        AnsiConsole.WriteLine();

        // Format flags
        AnsiConsole.MarkupLine("  [grey]Formatting flags[/]");
        RenderFlag(resume.Format, ResumeFormatFlags.HasMultipleColumns, "Multi-column layout", warn: true);
        RenderFlag(resume.Format, ResumeFormatFlags.HasTables,          "Tables",              warn: true);
        RenderFlag(resume.Format, ResumeFormatFlags.HasImages,          "Images / graphics",   warn: true);
        RenderFlag(resume.Format, ResumeFormatFlags.HasHeaders,         "Document header",     warn: false);
        RenderFlag(resume.Format, ResumeFormatFlags.HasFooters,         "Document footer",     warn: false);

        AnsiConsole.WriteLine();

        // Sections
        AnsiConsole.MarkupLine("  [grey]Detected sections[/]");
        if (resume.Sections.Any())
        {
            foreach (var section in resume.Sections)
            {
                var typeLabel = section.Type == SectionType.Unknown
                    ? "[grey](unrecognised)[/]"
                    : $"[dim]{section.Type}[/]";
                var bulletNote = section.BulletPoints.Count > 0
                    ? $" [grey]· {section.BulletPoints.Count} bullet{(section.BulletPoints.Count == 1 ? "" : "s")}[/]"
                    : "";
                AnsiConsole.MarkupLine($"    [green]✔[/] {Markup.Escape(section.Title)} {typeLabel}{bulletNote}");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("    [yellow]⚠[/] No sections detected — check that your section headers are on their own lines.");
        }
    }

    private static void RenderFlag(ResumeFormatFlags flags, ResumeFormatFlags flag, string label, bool warn)
    {
        if (flags.HasFlag(flag))
        {
            var icon = warn ? "[yellow]⚠[/]" : "[grey]–[/]";
            AnsiConsole.MarkupLine($"    {icon}  {label}: [yellow]detected[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"    [green]✔[/]  {label}: not detected");
        }
    }

    private static void RenderPositiveChecklist(ParsedResume resume, ScanResult result)
    {
        var goods = new List<string>();

        if (!resume.Format.HasFlag(ResumeFormatFlags.HasMultipleColumns))
            goods.Add("Single-column layout");
        if (!resume.Format.HasFlag(ResumeFormatFlags.HasTables))
            goods.Add("No tables");
        if (!resume.Format.HasFlag(ResumeFormatFlags.HasImages))
            goods.Add("No images or graphics");
        if (resume.Contact.Email is not null)
            goods.Add("Email address detected");
        if (resume.Contact.Phone is not null)
            goods.Add("Phone number detected");
        if (result.DetectedSections.Contains(SectionType.Experience))
            goods.Add("Experience section found");
        if (result.DetectedSections.Contains(SectionType.Education))
            goods.Add("Education section found");
        if (result.DetectedSections.Contains(SectionType.Skills))
            goods.Add("Skills section found");
        if (result.DetectedSections.Contains(SectionType.Summary) ||
            result.DetectedSections.Contains(SectionType.Objective))
            goods.Add("Summary / objective present");
        if (resume.Contact.LinkedIn is not null)
            goods.Add("LinkedIn URL present");
        if (resume.Contact.GitHub is not null)
            goods.Add("GitHub URL present");

        if (goods.Count == 0) return;

        AnsiConsole.MarkupLine("  [green]What looks good[/]");
        foreach (var item in goods)
            AnsiConsole.MarkupLine($"    [green]✔[/] {item}");
    }

    private static void RenderIssuesByCategory(ScanResult result)
    {
        var byCategory = result.Issues
            .OrderBy(i => i.Severity)
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Min(i => i.Severity));

        AnsiConsole.MarkupLine("  [red]Issues to fix[/]");

        foreach (var group in byCategory)
        {
            var critical = group.Count(i => i.Severity == IssueSeverity.Critical);
            var warning  = group.Count(i => i.Severity == IssueSeverity.Warning);
            var info     = group.Count(i => i.Severity == IssueSeverity.Info);

            var countParts = new List<string>();
            if (critical > 0) countParts.Add($"[red]{critical} critical[/]");
            if (warning  > 0) countParts.Add($"[yellow]{warning} warning[/]");
            if (info     > 0) countParts.Add($"[grey]{info} info[/]");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [bold]{group.Key}[/]  {string.Join("  ", countParts)}");

            foreach (var issue in group)
            {
                var (icon, color) = IssueStyle(issue.Severity);
                AnsiConsole.MarkupLine($"    [{color}]{icon}[/] {Markup.Escape(issue.Message)}");
                if (issue.Suggestion is not null)
                {
                    // In verbose mode, wrap the suggestion text for readability
                    AnsiConsole.MarkupLine($"      [grey]What to do:[/]");
                    AnsiConsole.MarkupLine($"      [grey]{Markup.Escape(issue.Suggestion)}[/]");
                }
            }
        }
    }

    private static void RenderPriorityActions(IReadOnlyList<ScanResult> results)
    {
        // Collect unique critical issues across all platforms, ranked by how many platforms flag them
        var criticals = results
            .SelectMany(r => r.Issues.Where(i => i.Severity == IssueSeverity.Critical)
                .Select(i => (i.Message, i.Suggestion)))
            .GroupBy(x => x.Message)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .ToList();

        if (criticals.Count == 0) return;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red]Top Priority Actions[/]").RuleStyle("red dim"));
        AnsiConsole.MarkupLine("  [grey]These critical issues affect the most platforms — fix them first.[/]");
        AnsiConsole.WriteLine();

        int rank = 1;
        foreach (var group in criticals)
        {
            var suggestion = group.First().Suggestion;
            var platformCount = group.Count();
            var affected = platformCount == 1 ? "1 platform" : $"{platformCount} platforms";

            AnsiConsole.MarkupLine($"  [red bold]{rank}.[/] {Markup.Escape(group.Key)}  [grey]({affected})[/]");
            if (suggestion is not null)
                AnsiConsole.MarkupLine($"     [grey]{Markup.Escape(suggestion)}[/]");
            AnsiConsole.WriteLine();
            rank++;
        }
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static Table BuildSummaryTable(IReadOnlyList<ScanResult> results)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Platform")
            .AddColumn("Score")
            .AddColumn("Rating")
            .AddColumn("Issues");

        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            var scoreColor = ScoreColor(result.Score);
            var profileName = ProfileRegistry.Get(result.Platform).DisplayName;
            var criticalCount = result.Issues.Count(i => i.Severity == IssueSeverity.Critical);
            var warningCount  = result.Issues.Count(i => i.Severity == IssueSeverity.Warning);
            var infoCount     = result.Issues.Count(i => i.Severity == IssueSeverity.Info);

            var issuesSummary = string.Join("  ",
                new[]
                {
                    criticalCount > 0 ? $"[red]{criticalCount} critical[/]" : "",
                    warningCount  > 0 ? $"[yellow]{warningCount} warning[/]" : "",
                    infoCount     > 0 ? $"[grey]{infoCount} info[/]" : ""
                }.Where(s => s.Length > 0));

            if (issuesSummary.Length == 0) issuesSummary = "[green]None[/]";

            table.AddRow(
                profileName,
                $"[{scoreColor}]{result.Score}/100[/]",
                $"[{scoreColor}]{result.Rating}[/]",
                issuesSummary);
        }

        return table;
    }

    private static void RenderSectionLists(ScanResult result)
    {
        if (result.DetectedSections.Any())
            AnsiConsole.MarkupLine($"\n  [green]✔ Detected sections:[/] {string.Join(", ", result.DetectedSections)}");
        if (result.MissingSections.Any())
            AnsiConsole.MarkupLine($"  [red]✖ Missing sections:[/]  {string.Join(", ", result.MissingSections)}");
    }

    private static string ScoreBadge(ScanResult result) =>
        $"[{ScoreColor(result.Score)}]{result.Score}/100 — {result.Rating}[/]";

    private static string ScoreColor(int score) => score switch
    {
        >= 90 => "green",
        >= 75 => "chartreuse2",
        >= 55 => "yellow",
        >= 35 => "darkorange",
        _ => "red"
    };

    private static (string icon, string color) IssueStyle(IssueSeverity severity) => severity switch
    {
        IssueSeverity.Critical => ("✖", "red"),
        IssueSeverity.Warning  => ("⚠", "yellow"),
        _                      => ("ℹ", "grey")
    };
}

