namespace ConsoleRender.Tests;

public class CodeHighlighterTests
{
    private static readonly CodeHighlighter Cs = new(CodeLanguage.CSharp);
    private static readonly CodeHighlighter Json = new(CodeLanguage.Json);
    private static readonly CodeHighlighter Sh = new(CodeLanguage.Shell);

    private static IReadOnlyList<HighlightSpan> Line(CodeHighlighter highlighter, string line)
    {
        return highlighter.Highlight([line])[0];
    }

    [Fact]
    public void KeywordsAreColoredButOtherIdentifiersAreNot()
    {
        var spans = Line(Cs, "public class Foo");

        Assert.Equal(
        [
            new HighlightSpan(0, 6, Cs.KeywordColor, CellStyle.None),
            new HighlightSpan(7, 5, Cs.KeywordColor, CellStyle.None),
        ], spans);
    }

    [Fact]
    public void AKeywordInsideALongerIdentifierIsNotAKeyword()
    {
        Assert.Empty(Line(Cs, "classic forEach _if"));
    }

    [Fact]
    public void AStringRunsToItsClosingQuoteAndSkipsEscapes()
    {
        var spans = Line(Cs, "x = \"a\\\"b\" + y");

        Assert.Equal([new HighlightSpan(4, 6, Cs.StringColor, CellStyle.None)], spans);
    }

    [Fact]
    public void AnUnterminatedStringEndsAtTheLineEnd()
    {
        Assert.Equal([new HighlightSpan(0, 4, Cs.StringColor, CellStyle.None)], Line(Cs, "\"abc"));
    }

    [Fact]
    public void KeywordsInsideStringsStayString()
    {
        Assert.Equal([new HighlightSpan(0, 8, Cs.StringColor, CellStyle.None)], Line(Cs, "\"return\""));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("0x1F")]
    [InlineData("1.5f")]
    [InlineData("1_000")]
    public void NumbersIncludeHexSuffixesAndSeparators(string number)
    {
        Assert.Equal([new HighlightSpan(0, number.Length, Cs.NumberColor, CellStyle.None)], Line(Cs, number));
    }

    [Fact]
    public void ALineCommentRunsToTheEnd()
    {
        var spans = Line(Cs, "x; // return \"s\"");

        Assert.Equal([new HighlightSpan(3, 13, Cs.CommentColor, CellStyle.Italic)], spans);
    }

    [Fact]
    public void ABlockCommentSpansLines()
    {
        var doc = Cs.Highlight(["a /* one", "two", "three */ if"]);

        Assert.Equal([new HighlightSpan(2, 6, Cs.CommentColor, CellStyle.Italic)], doc[0]);
        Assert.Equal([new HighlightSpan(0, 3, Cs.CommentColor, CellStyle.Italic)], doc[1]);
        Assert.Equal(
        [
            new HighlightSpan(0, 8, Cs.CommentColor, CellStyle.Italic),
            new HighlightSpan(9, 2, Cs.KeywordColor, CellStyle.None),
        ], doc[2]);
    }

    [Fact]
    public void ABlockCommentClosedOnTheSameLineLeavesTheRestAsCode()
    {
        var spans = Line(Cs, "/* c */ var");

        Assert.Equal(new HighlightSpan(8, 3, Cs.KeywordColor, CellStyle.None), spans[1]);
    }

    [Fact]
    public void JsonKeysAndValuesGetDifferentColors()
    {
        var spans = Line(Json, "  \"name\" : \"x\", \"on\": true");

        Assert.Equal(
        [
            new HighlightSpan(2, 6, Json.KeyColor, CellStyle.None),
            new HighlightSpan(11, 3, Json.StringColor, CellStyle.None),
            new HighlightSpan(16, 4, Json.KeyColor, CellStyle.None),
            new HighlightSpan(22, 4, Json.KeywordColor, CellStyle.None),
        ], spans);
    }

    [Fact]
    public void ShellVariablesAreRecognizedInEveryForm()
    {
        var spans = Line(Sh, "echo $HOME ${USER} $1 $?");

        Assert.Equal(
        [
            new HighlightSpan(5, 5, Sh.VariableColor, CellStyle.None),
            new HighlightSpan(11, 7, Sh.VariableColor, CellStyle.None),
            new HighlightSpan(19, 2, Sh.VariableColor, CellStyle.None),
            new HighlightSpan(22, 2, Sh.VariableColor, CellStyle.None),
        ], spans);
    }

    [Fact]
    public void AShellHashIsACommentOnlyAtAWordStart()
    {
        Assert.Equal([new HighlightSpan(0, 7, Sh.VariableColor, CellStyle.None)], Line(Sh, "${#arr}"));
        Assert.Equal([new HighlightSpan(2, 3, Sh.CommentColor, CellStyle.Italic)], Line(Sh, "a # c"));
        Assert.Empty(Line(Sh, "a#b"));
    }

    [Theory]
    [InlineData("csharp", "C#")]
    [InlineData("CS", "C#")]
    [InlineData("jsonc", "JSON")]
    [InlineData("bash", "Shell")]
    public void LanguagesAreFoundByAliasIgnoringCase(string alias, string name)
    {
        Assert.True(CodeLanguage.TryFind(alias, out var language));
        Assert.Equal(name, language.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("cobol")]
    public void ForFenceReturnsNullForUnknownOrMissingLanguages(string info)
    {
        Assert.Null(SyntaxHighlighters.ForFence(info));
    }

    [Fact]
    public void ForFenceUsesOnlyTheFirstWordOfTheInfoString()
    {
        var highlighter = Assert.IsType<CodeHighlighter>(SyntaxHighlighters.ForFence(" json title=\"x\""));

        Assert.Same(CodeLanguage.Json, highlighter.Language);
    }

    [Fact]
    public void SpansAreAlwaysSortedAndFreeOfOverlaps()
    {
        var samples = new (CodeHighlighter Highlighter, string[] Lines)[]
        {
            (Cs, ["var s = \"/* nope */\"; /* yes */ int x = 0x10; // \"tail\"", "'c' + @\"v\""]),
            (Json, ["{ \"a\": [1, 2.5, -3], \"b\": { \"c\": null } } // c"]),
            (Sh, ["for f in *.txt; do echo \"$f\" ${#f} $@; done # end"]),
        };

        foreach (var (highlighter, lines) in samples)
        {
            foreach (var lineSpans in highlighter.Highlight(lines))
            {
                var previousEnd = -1;
                foreach (var span in lineSpans)
                {
                    Assert.True(span.Start >= previousEnd, $"Span at {span.Start} overlaps or is out of order.");
                    Assert.True(span.Length > 0);
                    previousEnd = span.Start + span.Length;
                }
            }
        }
    }
}
