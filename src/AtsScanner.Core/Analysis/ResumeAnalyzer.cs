using AtsScanner.Core.Models;
using AtsScanner.Core.Parsing;
using AtsScanner.Core.Profiles;

namespace AtsScanner.Core.Analysis;

public sealed class ResumeAnalyzer
{
    /// <summary>
    /// Parses the resume file and runs it through every registered ATS platform profile.
    /// </summary>
    public async Task<IReadOnlyList<ScanResult>> AnalyzeAllAsync(
        string filePath, CancellationToken ct = default)
    {
        var resume = await ParseAsync(filePath, ct);
        return ProfileRegistry.GetAll().Select(p => p.Analyze(resume)).ToList();
    }

    /// <summary>
    /// Parses the resume file and runs it through the specified ATS platform profile.
    /// </summary>
    public async Task<ScanResult> AnalyzeAsync(
        string filePath, AtsPlatform platform, CancellationToken ct = default)
    {
        var resume = await ParseAsync(filePath, ct);
        return ProfileRegistry.Get(platform).Analyze(resume);
    }

    /// <summary>Parses only — useful for inspecting the normalized model before scoring.</summary>
    public Task<ParsedResume> ParseAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Resume file not found: {filePath}");

        var parser = ResumeParserFactory.GetParser(filePath);
        return parser.ParseAsync(filePath, ct);
    }
}
