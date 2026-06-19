using AtsScanner.Core.Models;

namespace AtsScanner.Core.Parsing;

public static class ResumeParserFactory
{
    private static readonly IReadOnlyList<IResumeParser> Parsers =
    [
        new PdfResumeParser(),
        new DocxResumeParser()
    ];

    public static IResumeParser GetParser(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return Parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(ext))
            ?? throw new NotSupportedException(
                $"No parser available for '{ext}'. Supported: {string.Join(", ", Parsers.SelectMany(p => p.SupportedExtensions))}");
    }
}
