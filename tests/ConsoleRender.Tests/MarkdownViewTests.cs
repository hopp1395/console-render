namespace ConsoleRender.Tests;

public class MarkdownViewTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0')
    {
        return new(ch, key, false, false, false);
    }

    private static ConsoleBuffer Render(MarkdownView view, int width, int height)
    {
        var app = new ConsoleApp();
        view.Parent?.Remove(view);
        view.Left = 0;
        view.Top = 0;
        view.Width = width;
        view.Height = height;
        app.Root.Add(view);
        return app.RenderOffscreen(width, height);
    }

    private static string[] Rows(ConsoleBuffer buffer)
    {
        return buffer.ToText().Split('\n').Select(row => row.TrimEnd()).ToArray();
    }

    private static int Column(ConsoleBuffer buffer, int row, string text)
    {
        return Rows(buffer)[row].IndexOf(text, StringComparison.Ordinal);
    }

    [Fact]
    public void InlineMarkersAreRemovedAndTheirStylesApplied()
    {
        var view = new MarkdownView { Markdown = "a **fett** *kursiv* `code` ~~weg~~" };

        var buffer = Render(view, 40, 1);

        Assert.Equal("a fett kursiv code weg", Rows(buffer)[0]);
        Assert.True(buffer[2, 0].Style.HasFlag(CellStyle.Bold));
        Assert.True(buffer[Column(buffer, 0, "kursiv"), 0].Style.HasFlag(CellStyle.Italic));
        Assert.Equal(view.CodeColor, buffer[Column(buffer, 0, "code"), 0].Foreground);
        Assert.True(buffer[Column(buffer, 0, "weg"), 0].Style.HasFlag(CellStyle.Strikethrough));
    }

    [Fact]
    public void LinksShowTheirTextUnderlinedAndHideTheUrl()
    {
        var view = new MarkdownView { Markdown = "siehe [Doku](https://example.org) hier" };

        var buffer = Render(view, 40, 1);

        Assert.Equal("siehe Doku hier", Rows(buffer)[0]);
        Assert.Equal(view.LinkTextColor, buffer[6, 0].Foreground);
        Assert.True(buffer[6, 0].Style.HasFlag(CellStyle.Underline));
    }

    [Fact]
    public void HeadingsLoseTheirHashesAndAreSeparatedFromTheText()
    {
        var view = new MarkdownView { Markdown = "# Titel\nText\n## Unter" };

        var buffer = Render(view, 20, 5);
        var rows = Rows(buffer);

        Assert.Equal(["Titel", "", "Text", "", "Unter"], rows);
        Assert.Equal(CellStyle.Bold | CellStyle.Underline, buffer[0, 0].Style);
        Assert.Equal(view.HeadingColor, buffer[0, 0].Foreground);
        Assert.Equal(CellStyle.Bold, buffer[0, 4].Style);
    }

    [Fact]
    public void ParagraphLinesAreJoinedAndWrappedWithTheirStyles()
    {
        var view = new MarkdownView { Markdown = "eins zwei\n**drei vier** fuenf" };

        var buffer = Render(view, 10, 3);

        Assert.Equal(["eins zwei", "drei vier", "fuenf"], Rows(buffer));
        Assert.True(buffer[0, 1].Style.HasFlag(CellStyle.Bold));
        Assert.True(buffer[5, 1].Style.HasFlag(CellStyle.Bold));
        Assert.False(buffer[0, 2].Style.HasFlag(CellStyle.Bold));
    }

    [Fact]
    public void AWordLongerThanTheRowIsBrokenHard()
    {
        var view = new MarkdownView { Markdown = "abcdefghij" };

        Assert.Equal(["abcd", "efgh", "ij"], Rows(Render(view, 4, 3)));
    }

    [Fact]
    public void ListItemsGetBulletsAndHangingIndents()
    {
        var view = new MarkdownView { Markdown = "- erster Punkt lang\n  - innen\n3. drei" };

        var buffer = Render(view, 12, 5);

        Assert.Equal(["• erster", "  Punkt lang", "  • innen", "3. drei", ""], Rows(buffer));
        Assert.Equal(view.BulletColor, buffer[0, 0].Foreground);
    }

    [Fact]
    public void QuotesGetAGutterAndGrayItalicText()
    {
        var view = new MarkdownView { Markdown = "> zitiert *hier*\n> weiter\n>\n> zwei" };

        var buffer = Render(view, 30, 3);

        Assert.Equal(["│ zitiert hier weiter", "│", "│ zwei"], Rows(buffer));
        Assert.Equal(view.QuoteColor, buffer[2, 0].Foreground);
        Assert.True(buffer[2, 0].Style.HasFlag(CellStyle.Italic));
    }

    [Fact]
    public void CodeBlocksAreHighlightedOnTheirBackgroundAndNotWrapped()
    {
        var view = new MarkdownView { Markdown = "```csharp\nvar x = 1; // a long comment\n```" };

        var buffer = Render(view, 12, 1);
        var keyword = new CodeHighlighter(CodeLanguage.CSharp).KeywordColor;

        Assert.Equal(" var x = 1;", Rows(buffer)[0]);
        Assert.Equal(keyword, buffer[1, 0].Foreground);
        Assert.Equal(view.CodeBackground, buffer[11, 0].Background);
    }

    [Fact]
    public void AnUnknownFenceLanguageUsesTheCodeColor()
    {
        var view = new MarkdownView { Markdown = "```cobol\nMOVE A TO B\n```" };

        Assert.Equal(view.CodeColor, Render(view, 20, 1)[1, 0].Foreground);
    }

    [Fact]
    public void ARuleSpansTheWholeWidth()
    {
        var view = new MarkdownView { Markdown = "---" };

        Assert.Equal("──────────", Rows(Render(view, 10, 1))[0]);
    }

    [Fact]
    public void ScrollingIsClampedOnTheNextFrame()
    {
        var view = new MarkdownView { Markdown = "a\n\nb\n\nc\n\nd" };
        Render(view, 10, 3);

        view.OnKey(Key(ConsoleKey.End));
        Assert.Equal("d", Rows(Render(view, 10, 3))[2]);
        Assert.Equal(4, view.ScrollOffset);

        // Taller than the content: the end position heals back to the top.
        Render(view, 10, 20);
        Assert.Equal(0, view.ScrollOffset);
    }

    [Fact]
    public void ChangingTheTextOrWidthRelaysTheDocument()
    {
        var view = new MarkdownView { Markdown = "eins zwei" };
        Assert.Equal("eins zwei", Rows(Render(view, 20, 1))[0]);

        Assert.Equal("eins", Rows(Render(view, 5, 2))[0]);

        view.Markdown = "drei";
        Assert.Equal("drei", Rows(Render(view, 5, 2))[0]);
    }
}
