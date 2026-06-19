using AtsScanner.Core.Models;

namespace AtsScanner.Core.Parsing;

public interface IResumeParser
{
    /// <summary>File extensions this parser handles, e.g. ".pdf".</summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    Task<ParsedResume> ParseAsync(string filePath, CancellationToken ct = default);
}
