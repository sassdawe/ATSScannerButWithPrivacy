using AtsScanner.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<ScanCommand>();

app.Configure(config =>
{
    config.SetApplicationName("ats-scanner");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<ScanCommand>("scan")
        .WithDescription("Scan a resume (.pdf or .docx) against one or all ATS platforms.");

    config.SetExceptionHandler((ex, _) =>
    {
        Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        return -1;
    });
});

return await app.RunAsync(args);
