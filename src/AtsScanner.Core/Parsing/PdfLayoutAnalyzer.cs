namespace AtsScanner.Core.Parsing;

/// <summary>
/// A word's text and bounding box, expressed in PDF coordinate space (origin bottom-left,
/// Y increases upward). Used to reconstruct reading order and detect layout characteristics
/// without depending directly on a PDF parsing library, so the logic can be unit tested with
/// synthetic layouts.
/// </summary>
public readonly record struct PdfWordBox(string Text, double Left, double Right, double Top, double Bottom);

/// <summary>
/// Reconstructs line-based text and detects layout features (such as genuine multi-column
/// layouts) from raw word positions extracted from a PDF page. PDF text extraction does not
/// preserve line breaks on its own, so grouping words by vertical position is required to
/// recover a line structure that section-header detection can work with.
/// </summary>
public static class PdfLayoutAnalyzer
{
    // Minimum horizontal gap (in PDF points) between two word clusters on the same line
    // before it's considered a potential column gutter rather than ordinary word/sentence spacing.
    private const double GutterThreshold = 24.0;

    // A gutter position must recur at roughly the same X coordinate across multiple lines
    // (within this tolerance) to count as a consistent column boundary.
    private const double GutterPositionTolerance = 20.0;

    // Minimum number of distinct lines that must share a consistent gutter position before the
    // page is treated as multi-column. Guards against a one-off wide gap (e.g. a table row).
    private const int MinLinesWithGutter = 4;

    /// <summary>
    /// Groups words into lines by vertical position and joins them left-to-right, top-to-bottom,
    /// producing text with real line breaks between visual lines.
    /// </summary>
    public static string BuildText(IReadOnlyList<PdfWordBox> words)
    {
        var lines = GroupIntoLines(words);
        return string.Join('\n', lines.Select(line =>
            string.Join(' ', line.OrderBy(w => w.Left).Select(w => w.Text))));
    }

    /// <summary>
    /// Detects a genuine multi-column layout: a vertical gutter (empty gap) between two blocks
    /// of text that recurs at the same horizontal position across several distinct lines.
    /// Ordinary wrapped single-column paragraphs span the full page width without a consistent gap.
    /// </summary>
    public static bool DetectMultipleColumns(IReadOnlyList<PdfWordBox> words)
    {
        var lines = GroupIntoLines(words);

        var gutterPositions = new List<double>();
        foreach (var line in lines)
        {
            var ordered = line.OrderBy(w => w.Left).ToList();
            if (ordered.Count < 2) continue;

            double bestGap = 0;
            double bestMidpoint = 0;
            for (var i = 1; i < ordered.Count; i++)
            {
                var gap = ordered[i].Left - ordered[i - 1].Right;
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestMidpoint = (ordered[i].Left + ordered[i - 1].Right) / 2;
                }
            }

            if (bestGap >= GutterThreshold)
                gutterPositions.Add(bestMidpoint);
        }

        if (gutterPositions.Count < MinLinesWithGutter) return false;

        // Find the largest cluster of gutter positions within tolerance of each other.
        gutterPositions.Sort();
        var maxClusterSize = 1;
        var clusterStart = 0;
        for (var i = 1; i < gutterPositions.Count; i++)
        {
            while (gutterPositions[i] - gutterPositions[clusterStart] > GutterPositionTolerance)
                clusterStart++;

            maxClusterSize = Math.Max(maxClusterSize, i - clusterStart + 1);
        }

        return maxClusterSize >= MinLinesWithGutter;
    }

    private static List<List<PdfWordBox>> GroupIntoLines(IReadOnlyList<PdfWordBox> words)
    {
        var lines = new List<List<PdfWordBox>>();
        if (words.Count == 0) return lines;

        // Reading order: top of page first (highest Top value), then left to right.
        var sorted = words.OrderByDescending(w => w.Top).ThenBy(w => w.Left).ToList();

        List<PdfWordBox>? currentLine = null;
        double currentLineY = 0;

        foreach (var word in sorted)
        {
            var center = (word.Top + word.Bottom) / 2;
            var height = Math.Max(word.Top - word.Bottom, 1);

            if (currentLine is not null && Math.Abs(center - currentLineY) <= height * 0.6)
            {
                currentLine.Add(word);
            }
            else
            {
                currentLine = [word];
                lines.Add(currentLine);
                currentLineY = center;
            }
        }

        return lines;
    }
}
