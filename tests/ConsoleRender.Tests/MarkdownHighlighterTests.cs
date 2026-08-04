namespace ConsoleRender.Tests;

public class MarkdownHighlighterTests
{
    private static readonly MarkdownHighlighter Md = new();

    private static IReadOnlyList<HighlightSpan> Line(string line) => Md.Highlight([line])[0];

    [Fact]
    public void AHeadingDimsTheHashesAndBoldsTheTitle()
    {
        var spans = Line("## Titel");

        Assert.Equal(new HighlightSpan(0, 2, Color.Default, CellStyle.Dim), spans[0]);
        Assert.Equal(new HighlightSpan(3, 5, Md.HeadingColor, CellStyle.Bold), spans[1]);
    }

    [Fact]
    public void SevenHashesAreNotAHeading()
    {
        Assert.Empty(Line("####### zu tief"));
    }

    [Fact]
    public void AHashWithoutASpaceIsNotAHeading()
    {
        Assert.Empty(Line("#hashtag"));
    }

    [Fact]
    public void BoldMarksTheMarkersDimAndTheContentBold()
    {
        var spans = Line("a **fett** b");

        Assert.Equal(new HighlightSpan(2, 2, Color.Default, CellStyle.Dim), spans[0]);
        Assert.Equal(new HighlightSpan(4, 4, Color.Default, CellStyle.Bold), spans[1]);
        Assert.Equal(new HighlightSpan(8, 2, Color.Default, CellStyle.Dim), spans[2]);
    }

    [Fact]
    public void ItalicAndStrikethroughCarryTheirFlags()
    {
        Assert.Contains(new HighlightSpan(1, 6, Color.Default, CellStyle.Italic), Line("*kursiv*"));
        Assert.Contains(new HighlightSpan(2, 5, Color.Default, CellStyle.Strikethrough), Line("~~durch~~"));
    }

    [Fact]
    public void TripleAsterisksNestBoldAndItalic()
    {
        var spans = Line("***x***");

        Assert.Contains(new HighlightSpan(3, 1, Color.Default, CellStyle.Bold | CellStyle.Italic), spans);
    }

    [Fact]
    public void AnUnpairedMarkerStaysPlainText()
    {
        Assert.Empty(Line("2 ** 3 ist nicht fett"));
    }

    [Fact]
    public void ACodeSpanProtectsItsContentFromOtherMarkers()
    {
        var spans = Line("`**x**`");

        Assert.Equal(new HighlightSpan(0, 1, Color.Default, CellStyle.Dim), spans[0]);
        Assert.Equal(new HighlightSpan(1, 5, Md.CodeColor, CellStyle.None), spans[1]);
        Assert.Equal(new HighlightSpan(6, 1, Color.Default, CellStyle.Dim), spans[2]);
    }

    [Fact]
    public void ALinkUnderlinesTheTextAndDimsTheRest()
    {
        var spans = Line("[hier](https://x)");

        Assert.Equal(new HighlightSpan(0, 1, Color.Default, CellStyle.Dim), spans[0]);
        Assert.Equal(new HighlightSpan(1, 4, Md.LinkTextColor, CellStyle.Underline), spans[1]);
        Assert.Equal(new HighlightSpan(5, 2, Color.Default, CellStyle.Dim), spans[2]);
        Assert.Equal(new HighlightSpan(7, 9, Color.Default, CellStyle.Dim), spans[3]);
        Assert.Equal(new HighlightSpan(16, 1, Color.Default, CellStyle.Dim), spans[4]);
    }

    [Theory]
    [InlineData("- eins")]
    [InlineData("* eins")]
    [InlineData("+ eins")]
    public void BulletMarkersAreColored(string line)
    {
        Assert.Equal(new HighlightSpan(0, 1, Md.BulletColor, CellStyle.None), Line(line)[0]);
    }

    [Fact]
    public void OrderedListMarkersIncludeTheDot()
    {
        Assert.Equal(new HighlightSpan(0, 3, Md.BulletColor, CellStyle.None), Line("12. eins")[0]);
    }

    [Fact]
    public void AStarWithoutASpaceIsEmphasisNotAList()
    {
        var spans = Line("*kein*");

        Assert.Contains(new HighlightSpan(1, 4, Color.Default, CellStyle.Italic), spans);
    }

    [Fact]
    public void AQuoteStylesItsWholeText_AndInlineMarkersStillWork()
    {
        var spans = Line("> mit **fett** dabei");

        Assert.Equal(new HighlightSpan(0, 1, Color.Default, CellStyle.Dim), spans[0]);
        Assert.Contains(new HighlightSpan(2, 4, Md.QuoteColor, CellStyle.Italic), spans);
        Assert.Contains(new HighlightSpan(8, 4, Md.QuoteColor, CellStyle.Italic | CellStyle.Bold), spans);
        Assert.Contains(new HighlightSpan(14, 6, Md.QuoteColor, CellStyle.Italic), spans);
    }

    [Theory]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("___")]
    [InlineData("- - -")]
    public void RulesAreDimmedWholesale(string line)
    {
        Assert.Equal([new HighlightSpan(0, line.Length, Color.Default, CellStyle.Dim)], Line(line));
    }

    [Fact]
    public void ADashWithTextIsAListNotARule()
    {
        Assert.Equal(Md.BulletColor, Line("- item")[0].Foreground);
    }

    [Fact]
    public void FencedLinesAreCodeWithoutInlineAnalysis()
    {
        var doc = Md.Highlight(["```csharp", "var x = \"**nicht fett**\";", "```", "danach **fett**"]);

        Assert.Equal(new HighlightSpan(0, 3, Color.Default, CellStyle.Dim), doc[0][0]);
        Assert.Equal(new HighlightSpan(3, 6, Md.CodeColor, CellStyle.None), doc[0][1]);
        Assert.Equal([new HighlightSpan(0, 25, Md.CodeColor, CellStyle.None)], doc[1]);
        Assert.Contains(new HighlightSpan(9, 4, Color.Default, CellStyle.Bold), doc[3]);
    }

    [Fact]
    public void AnUnclosedFenceRunsToTheEndOfTheDocument()
    {
        var doc = Md.Highlight(["```", "# keine Überschrift", "text"]);

        Assert.Equal([new HighlightSpan(0, 19, Md.CodeColor, CellStyle.None)], doc[1]);
        Assert.Equal([new HighlightSpan(0, 4, Md.CodeColor, CellStyle.None)], doc[2]);
    }

    [Fact]
    public void SpansAreAlwaysSortedAndFreeOfOverlaps()
    {
        var doc = Md.Highlight(
        [
            "# Kopf **mit** allem",
            "> zitat *mit `code` und* ~~mehr~~",
            "- liste **fett *und kursiv* ende**",
            "[link](https://beispiel.de) und `code`",
            "***alles auf einmal*** --- kein rule",
            "```",
            "im fence",
        ]);

        foreach (var lineSpans in doc)
        {
            int previousEnd = -1;
            foreach (var span in lineSpans)
            {
                Assert.True(span.Start >= previousEnd, $"Span at {span.Start} overlaps or is out of order.");
                Assert.True(span.Length > 0);
                previousEnd = span.Start + span.Length;
            }
        }
    }
}
