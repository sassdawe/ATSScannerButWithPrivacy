using AtsScanner.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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

            // Detect multi-column layout using word positions
            if (!hasMultipleColumns)
                hasMultipleColumns = DetectMultipleColumns(page);

            rawTextBuilder.AppendLine(page.Text);
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

    private static bool DetectMultipleColumns(Page page)
    {
        var letters = page.Letters;
        if (letters.Count < 20) return false;

        var pageWidth = page.Width;
        // Use letter positions to detect side-by-side text blocks.
        var leftLetters = letters.Where(l => l.GlyphRectangle.Left < pageWidth * 0.45).ToList();
        var rightLetters = letters.Where(l => l.GlyphRectangle.Left > pageWidth * 0.55).ToList();

        if (leftLetters.Count < 10 || rightLetters.Count < 10)
            return false;

        // Check if left and right letters share overlapping Y ranges (same line area)
        var leftYRanges = leftLetters.Select(l => (l.GlyphRectangle.Bottom, l.GlyphRectangle.Top)).ToList();
        var rightYRanges = rightLetters.Select(l => (l.GlyphRectangle.Bottom, l.GlyphRectangle.Top)).ToList();

        int overlapCount = leftYRanges.Count(l =>
            rightYRanges.Any(r => l.Bottom < r.Top && r.Bottom < l.Top));

        return overlapCount > 5;
    }
}
