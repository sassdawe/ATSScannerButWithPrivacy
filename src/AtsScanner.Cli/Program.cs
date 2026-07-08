using AtsScanner.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("ats-scanner");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<ScanCommand>("scan")
        .WithDescription("Scan a resume (.pdf or .docx) against one or all ATS platforms.")
        .WithExample(["scan", "resume.pdf"])
        .WithExample(["scan", "resume.pdf", "--platform", "workday"])
        .WithExample(["scan", "resume.pdf", "--platform", "greenhouse", "--output", "json"])
        .WithExample(["scan", "resume.docx", "--platform", "all", "--verbose"])
        .WithAlias("s");

    config.SetExceptionHandler((ex, _) =>
    {
        Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        return -1;
    });
});

return await app.RunAsync(args);
