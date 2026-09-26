namespace ConsoleRender.Tests;

public class DiffHighlighterTests
{
    private static readonly DiffHighlighter Diff = new();

    [Fact]
    public void HeadersHunksAndChangedLinesGetTheirStyles()
    {
        var doc = Diff.Highlight(
        [
            "diff --git a/x.txt b/x.txt",
            "index 1111111..2222222 100644",
            "--- a/x.txt",
            "+++ b/x.txt",
            "@@ -1,2 +1,2 @@ void Main()",
            " same",
            "-old",
            "+new",
        ]);

        Assert.Equal([new HighlightSpan(0, 26, Diff.HeaderColor, CellStyle.Bold)], doc[0]);
        Assert.Equal([new HighlightSpan(0, 29, Color.Default, CellStyle.Dim)], doc[1]);
        Assert.Equal([new HighlightSpan(0, 11, Diff.HeaderColor, CellStyle.Bold)], doc[2]);
        Assert.Equal([new HighlightSpan(0, 11, Diff.HeaderColor, CellStyle.Bold)], doc[3]);
        // Only the "@@ … @@" part is colored; the function context after it stays plain.
        Assert.Equal([new HighlightSpan(0, 15, Diff.HunkColor, CellStyle.None)], doc[4]);
        Assert.Empty(doc[5]);
        Assert.Equal([new HighlightSpan(0, 4, Diff.RemovedColor, CellStyle.None)], doc[6]);
        Assert.Equal([new HighlightSpan(0, 4, Diff.AddedColor, CellStyle.None)], doc[7]);
    }

    [Fact]
    public void ARemovedLineThatLooksLikeAFileHeaderIsStillARemoval()
    {
        var doc = Diff.Highlight(["@@ -1,2 +1 @@", "--- not a header", "+++ still no header"]);

        Assert.Equal(Diff.RemovedColor, doc[1][0].Foreground);
        Assert.Equal(Diff.AddedColor, doc[2][0].Foreground);
    }

    [Fact]
    public void AfterTheHunkCountsRunOutHeadersAreHeadersAgain()
    {
        var doc = Diff.Highlight(["@@ -1 +1 @@", "-a", "+b", "--- a/next", "+++ b/next"]);

        Assert.Equal(CellStyle.Bold, doc[3][0].Style);
        Assert.Equal(CellStyle.Bold, doc[4][0].Style);
    }

    [Fact]
    public void TextBeforeTheFirstFileStaysPlain()
    {
        var doc = Diff.Highlight(["commit abc123", "Author: someone", "", "    Fix it", "diff --git a/x b/x"]);

        Assert.All(doc.Take(4), Assert.Empty);
    }

    [Fact]
    public void ANoNewlineMarkerIsDimmed()
    {
        var doc = Diff.Highlight(["@@ -1 +1 @@", "-a", "\\ No newline at end of file", "+b"]);

        Assert.Equal([new HighlightSpan(0, 27, Color.Default, CellStyle.Dim)], doc[2]);
        Assert.Equal(Diff.AddedColor, doc[3][0].Foreground);
    }

    [Theory]
    [InlineData("diff")]
    [InlineData("Patch")]
    public void DiffAndPatchFencesUseTheDiffHighlighter(string info)
    {
        Assert.IsType<DiffHighlighter>(SyntaxHighlighters.ForFence(info));
    }
}
