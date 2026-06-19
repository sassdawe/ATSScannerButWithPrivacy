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
        var analyzer = new ResumeAnalyzer();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Scanning [blue]{Path.GetFileName(filePath)}[/]...", async _ =>
            {
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

        if (!results.Any()) return 1;

        if (settings.Output.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            AnsiConsole.WriteLine(json);
            return 0;
        }

        RenderResults(Path.GetFileName(filePath), results);
        return 0;
    }

    private static void RenderResults(string fileName, IReadOnlyList<ScanResult> results)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("ATS Scanner").Color(Color.Blue));
        AnsiConsole.MarkupLine($"[grey]Resume:[/] [white]{fileName}[/]");
        AnsiConsole.WriteLine();

        // Summary table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Platform")
            .AddColumn("Score")
            .AddColumn("Rating")
            .AddColumn("Issues");

        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            var scoreColor = result.Score switch
            {
                >= 90 => "green",
                >= 75 => "chartreuse2",
                >= 55 => "yellow",
                >= 35 => "darkorange",
                _ => "red"
            };

            var profileName = ProfileRegistry.Get(result.Platform).DisplayName;
            var criticalCount = result.Issues.Count(i => i.Severity == IssueSeverity.Critical);
            var warningCount = result.Issues.Count(i => i.Severity == IssueSeverity.Warning);
            var infoCount = result.Issues.Count(i => i.Severity == IssueSeverity.Info);

            var issuesSummary = string.Join("  ",
                new[] {
                    criticalCount > 0 ? $"[red]{criticalCount} critical[/]" : "",
                    warningCount > 0 ? $"[yellow]{warningCount} warning[/]" : "",
                    infoCount > 0 ? $"[grey]{infoCount} info[/]" : ""
                }.Where(s => s.Length > 0));

            if (issuesSummary.Length == 0) issuesSummary = "[green]None[/]";

            table.AddRow(
                profileName,
                $"[{scoreColor}]{result.Score}/100[/]",
                $"[{scoreColor}]{result.Rating}[/]",
                issuesSummary);
        }

        AnsiConsole.Write(table);

        // Detailed issues per platform
        foreach (var result in results.OrderByDescending(r => r.Score))
        {
            if (!result.Issues.Any()) continue;

            var profileName = ProfileRegistry.Get(result.Platform).DisplayName;
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[blue]{profileName}[/] — Details").RuleStyle("blue dim"));

            foreach (var issue in result.Issues.OrderBy(i => i.Severity))
            {
                var (icon, color) = issue.Severity switch
                {
                    IssueSeverity.Critical => ("✖", "red"),
                    IssueSeverity.Warning => ("⚠", "yellow"),
                    _ => ("ℹ", "grey")
                };

                AnsiConsole.MarkupLine($"  [{color}]{icon} [{issue.Category}][/] {Markup.Escape(issue.Message)}");
                if (issue.Suggestion is not null)
                    AnsiConsole.MarkupLine($"    [grey]→ {Markup.Escape(issue.Suggestion)}[/]");
            }

            if (result.DetectedSections.Any())
            {
                AnsiConsole.MarkupLine($"  [green]✔ Detected sections:[/] {string.Join(", ", result.DetectedSections)}");
            }
            if (result.MissingSections.Any())
            {
                AnsiConsole.MarkupLine($"  [red]✖ Missing sections:[/] {string.Join(", ", result.MissingSections)}");
            }
        }

        AnsiConsole.WriteLine();
    }
}
