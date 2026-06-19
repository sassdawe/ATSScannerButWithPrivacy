using AtsScanner.Core.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AtsScanner.Core.Parsing;

public sealed class DocxResumeParser : IResumeParser
{
    public IReadOnlyList<string> SupportedExtensions { get; } = [".docx"];

    public Task<ParsedResume> ParseAsync(string filePath, CancellationToken ct = default)
    {
        using var wordDoc = WordprocessingDocument.Open(filePath, isEditable: false);
        var body = wordDoc.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException("DOCX has no document body.");

        ct.ThrowIfCancellationRequested();

        var rawText = ExtractText(body);
        var format = DetectFormat(wordDoc, body);
        var contact = ResumeTextAnalyzer.ExtractContactInfo(rawText);
        var sections = ResumeTextAnalyzer.ExtractSections(rawText);

        var resume = new ParsedResume(
            FileName: Path.GetFileName(filePath),
            FileFormat: "docx",
            RawText: rawText,
            Contact: contact,
            Sections: sections,
            Format: format
        );

        return Task.FromResult(resume);
    }

    private static string ExtractText(Body body)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var para in body.Descendants<Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }
        return sb.ToString();
    }

    private static ResumeFormatFlags DetectFormat(WordprocessingDocument doc, Body body)
    {
        var flags = ResumeFormatFlags.None;

        // Detect tables
        if (body.Descendants<Table>().Any())
            flags |= ResumeFormatFlags.HasTables;

        // Detect images/drawings
        var mainPart = doc.MainDocumentPart;
        if (mainPart is not null)
        {
            bool hasImages =
                mainPart.ImageParts.Any() ||
                body.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>().Any() ||
                body.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>().Any();

            if (hasImages)
                flags |= ResumeFormatFlags.HasImages;

            // Headers / Footers
            if (mainPart.HeaderParts.Any())
                flags |= ResumeFormatFlags.HasHeaders;

            if (mainPart.FooterParts.Any())
                flags |= ResumeFormatFlags.HasFooters;
        }

        // Detect multi-column sections via SectionProperties
        bool hasColumns = body
            .Descendants<Columns>()
            .Any(c => c.ColumnCount is not null && c.ColumnCount > 1);
        if (hasColumns)
            flags |= ResumeFormatFlags.HasMultipleColumns;

        return flags;
    }
}
