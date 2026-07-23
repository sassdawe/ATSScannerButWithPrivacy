using AtsScanner.Core.Models;
using UglyToad.PdfPig;

namespace AtsScanner.Core.Parsing;

public sealed class PdfResumeParser : IResumeParser
{
    public IReadOnlyList<string> SupportedExtensions { get; } = [".pdf"];

    public Task<ParsedResume> ParseAsync(string filePath, CancellationToken ct = default)
    {
        using var document = PdfDocument.Open(filePath);

        var rawTextBuilder = new System.Text.StringBuilder();
        bool hasMultipleColumns = false;
        bool hasImages = false;

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();

            // Detect images
            if (page.GetImages().Any())
                hasImages = true;

            var words = page.GetWords()
                .Select(w => new PdfWordBox(w.Text, w.BoundingBox.Left, w.BoundingBox.Right, w.BoundingBox.Top, w.BoundingBox.Bottom))
                .ToList();

            // Detect multi-column layout using word positions
            if (!hasMultipleColumns)
                hasMultipleColumns = PdfLayoutAnalyzer.DetectMultipleColumns(words);

            // PdfPig's Page.Text does not reliably preserve line breaks, which breaks section-header
            // detection (headers must be on their own line). Reconstruct lines from word positions instead.
            rawTextBuilder.AppendLine(PdfLayoutAnalyzer.BuildText(words));
        }

        var rawText = rawTextBuilder.ToString();
        var contact = ResumeTextAnalyzer.ExtractContactInfo(rawText);
        var sections = ResumeTextAnalyzer.ExtractSections(rawText);

        var format = ResumeFormatFlags.None;
        if (hasMultipleColumns) format |= ResumeFormatFlags.HasMultipleColumns;
        if (hasImages) format |= ResumeFormatFlags.HasImages;

        var resume = new ParsedResume(
            FileName: Path.GetFileName(filePath),
            FileFormat: "pdf",
            RawText: rawText,
            Contact: contact,
            Sections: sections,
            Format: format
        );

        return Task.FromResult(resume);
    }
}
