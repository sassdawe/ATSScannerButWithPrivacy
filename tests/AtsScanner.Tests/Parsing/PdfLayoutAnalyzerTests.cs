using AtsScanner.Core.Parsing;
using FluentAssertions;

namespace AtsScanner.Tests.Parsing;

public class PdfLayoutAnalyzerTests
{
    private static PdfWordBox Word(string text, double left, double right, double top, double bottom) =>
        new(text, left, right, top, bottom);

    [Fact]
    public void BuildText_GroupsWordsIntoLines_PreservingReadingOrder()
    {
        // Two visual lines at different vertical positions, words provided out of reading order.
        var words = new[]
        {
            Word("Summary", 200, 260, 700, 690),
            Word("Professional", 72, 195, 700, 690),
            Word("Competencies", 130, 260, 685, 675),
            Word("Core", 72, 125, 685, 675),
        };

        var text = PdfLayoutAnalyzer.BuildText(words);

        text.Should().Be("Professional Summary\nCore Competencies");
    }

    [Fact]
    public void BuildText_SectionHeaderOnOwnLine_IsDetectedAsSection()
    {
        // Simulates the real bug: PDF word extraction with no inherent line breaks must still
        // let ResumeTextAnalyzer detect "Professional Summary" as a standalone header line.
        var words = new[]
        {
            Word("Professional", 72, 195, 700, 690),
            Word("Summary", 200, 260, 700, 690),
            Word("Built", 72, 110, 685, 675),
            Word("great", 115, 160, 685, 675),
            Word("systems.", 165, 220, 685, 675),
            Word("Experience", 72, 160, 670, 660),
            Word("Acme", 72, 120, 655, 645),
            Word("Corp", 125, 165, 655, 645),
        };

        var text = PdfLayoutAnalyzer.BuildText(words);
        var sections = ResumeTextAnalyzer.ExtractSections(text);

        sections.Should().Contain(s => s.Title == "Professional Summary");
        sections.Should().Contain(s => s.Title == "Experience");
    }

    [Fact]
    public void DetectMultipleColumns_ReturnsFalse_ForWrappedSingleColumnParagraph()
    {
        // Ordinary paragraph text: each line spans nearly the full width with only normal
        // word spacing — no recurring gutter at a consistent horizontal position.
        var words = new List<PdfWordBox>();
        double top = 700;
        for (var line = 0; line < 10; line++)
        {
            double left = 72;
            for (var w = 0; w < 8; w++)
            {
                var right = left + 40;
                words.Add(Word($"word{w}", left, right, top, top - 10));
                left = right + 5; // normal word spacing
            }
            top -= 15;
        }

        PdfLayoutAnalyzer.DetectMultipleColumns(words).Should().BeFalse();
    }

    [Fact]
    public void DetectMultipleColumns_ReturnsTrue_ForConsistentTwoColumnLayout()
    {
        // A genuine two-column layout: a left block ending around X=250 and a right block
        // starting around X=310 (a 60pt gutter), repeated consistently across many lines.
        var words = new List<PdfWordBox>();
        double top = 700;
        for (var line = 0; line < 8; line++)
        {
            words.Add(Word("left", 72, 250, top, top - 10));
            words.Add(Word("right", 310, 480, top, top - 10));
            top -= 15;
        }

        PdfLayoutAnalyzer.DetectMultipleColumns(words).Should().BeTrue();
    }

    [Fact]
    public void DetectMultipleColumns_ReturnsFalse_ForOccasionalWideGap()
    {
        // Only a couple of lines have a wide gap (e.g. a single indented line) — not enough
        // to be treated as a real recurring column structure.
        var words = new List<PdfWordBox>
        {
            Word("left", 72, 250, 700, 690),
            Word("right", 310, 480, 700, 690),
            Word("left", 72, 250, 685, 675),
            Word("right", 310, 480, 685, 675),
        };
        for (var line = 0; line < 10; line++)
        {
            words.Add(Word("paragraph text spanning most of the line width here", 72, 480, 670 - line * 15, 660 - line * 15));
        }

        PdfLayoutAnalyzer.DetectMultipleColumns(words).Should().BeFalse();
    }
}
