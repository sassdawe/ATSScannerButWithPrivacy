using AtsScanner.Core.Models;
using AtsScanner.Core.Profiles;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace AtsScanner.Cli.Commands;

public sealed class ScanSettings : CommandSettings
{
    [CommandArgument(0, "<resume>")]
    [Description("Path to the resume file (.pdf or .docx)")]
    public string ResumePath { get; init; } = "";

    [CommandOption("-p|--platform")]
    [Description("ATS platform to scan against: all, workday, greenhouse, taleo, lever, successfactors, umantis")]
    [DefaultValue("all")]
    public string Platform { get; init; } = "all";

    [CommandOption("-o|--output")]
    [Description("Output format: text, json")]
    [DefaultValue("text")]
    public string Output { get; init; } = "text";

    [CommandOption("-v|--verbose")]
    [Description("Show expanded results: document analysis, positive checklist, and issues grouped by category")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }
}
