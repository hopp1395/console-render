namespace ConsoleRender.Tests;

public class TextAreaTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', ConsoleModifiers modifiers = 0)
    {
        return new(ch, key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));
    }

    private static ConsoleBuffer Render(TextArea area, int width, int height, bool focus = true)
    {
        var app = new ConsoleApp();
        // Some tests render the same instance twice; detach it from the previous host first.
        area.Parent?.Remove(area);
        area.Left = 0;
        area.Top = 0;
        area.Width = width;
        area.Height = height;
        app.Root.Add(area);
        if (focus)
        {
            app.SetFocus(area);
        }

        return app.RenderOffscreen(width, height);
    }

    [Fact]
    public void EnterSplitsTheLineAtTheCursor()
    {
        var area = new TextArea { Text = "abcd" };
        area.OnKey(Key(ConsoleKey.LeftArrow));
        area.OnKey(Key(ConsoleKey.LeftArrow));

        Assert.True(area.OnKey(Key(ConsoleKey.Enter)));

        Assert.Equal("ab\ncd", area.Text);
        Assert.Equal(1, area.CursorLine);
        Assert.Equal(0, area.CursorColumn);
    }

    [Fact]
    public void BackspaceAtColumnZeroMergesWithThePreviousLine()
    {
        var area = new TextArea { Text = "ab\ncd" };
        area.OnKey(Key(ConsoleKey.Home));

        area.OnKey(Key(ConsoleKey.Backspace));

        Assert.Equal("abcd", area.Text);
        Assert.Equal(0, area.CursorLine);
        Assert.Equal(2, area.CursorColumn); // at the seam
    }

    [Fact]
    public void DeleteAtTheLineEndPullsTheNextLineUp()
    {
        var area = new TextArea { Text = "ab\ncd" };
        area.OnKey(Key(ConsoleKey.UpArrow));
        area.OnKey(Key(ConsoleKey.End));

        area.OnKey(Key(ConsoleKey.Delete));

        Assert.Equal("abcd", area.Text);
    }

    [Fact]
    public void BackspaceAtTheDocumentStartIsANoOpButConsumed()
    {
        var area = new TextArea { Text = "ab" };
        area.OnKey(Key(ConsoleKey.Home, modifiers: ConsoleModifiers.Control));

        Assert.True(area.OnKey(Key(ConsoleKey.Backspace)));
        Assert.Equal("ab", area.Text);
    }

    [Fact]
    public void ArrowsCrossLineBoundaries()
    {
        var area = new TextArea { Text = "ab\ncd" };
        area.OnKey(Key(ConsoleKey.UpArrow));
        area.OnKey(Key(ConsoleKey.End));

        area.OnKey(Key(ConsoleKey.RightArrow));
        Assert.Equal((1, 0), (area.CursorLine, area.CursorColumn));

        area.OnKey(Key(ConsoleKey.LeftArrow));
        Assert.Equal((0, 2), (area.CursorLine, area.CursorColumn));
    }

    [Fact]
    public void VerticalMovementKeepsTheDesiredColumn()
    {
        var area = new TextArea { Text = "abcdef\nxy\nabcdef" };
        area.OnKey(Key(ConsoleKey.End)); // line 2, column 6

        area.OnKey(Key(ConsoleKey.UpArrow));   // short line clamps
        Assert.Equal((1, 2), (area.CursorLine, area.CursorColumn));

        area.OnKey(Key(ConsoleKey.UpArrow));   // sticky column returns
        Assert.Equal((0, 6), (area.CursorLine, area.CursorColumn));
    }

    [Fact]
    public void CtrlHomeAndCtrlEndJumpAcrossTheDocument()
    {
        var area = new TextArea { Text = "ab\ncd\nef" };

        area.OnKey(Key(ConsoleKey.Home, modifiers: ConsoleModifiers.Control));
        Assert.Equal((0, 0), (area.CursorLine, area.CursorColumn));

        area.OnKey(Key(ConsoleKey.End, modifiers: ConsoleModifiers.Control));
        Assert.Equal((2, 2), (area.CursorLine, area.CursorColumn));
    }

    [Fact]
    public void TabAndEscapeAreNotConsumed()
    {
        var area = new TextArea();

        Assert.False(area.OnKey(Key(ConsoleKey.Tab, '\t')));
        Assert.False(area.OnKey(Key(ConsoleKey.Escape)));
    }

    [Fact]
    public void TheTextSetterNormalizesWindowsLineBreaks()
    {
        var area = new TextArea { Text = "ab\r\ncd\rex" };

        Assert.Equal("ab\ncd\nex", area.Text);
    }

    [Fact]
    public void InsertingMultilineTextSplicesItAtTheCursor()
    {
        var area = new TextArea { Text = "start-end" };
        for (var i = 0; i < 4; i++)
        {
            area.OnKey(Key(ConsoleKey.LeftArrow)); // before "-end"... at column 5
        }

        area.Insert("1\n2");

        Assert.Equal("start1\n2-end", area.Text);
        Assert.Equal((1, 1), (area.CursorLine, area.CursorColumn));
    }

    [Fact]
    public void TextChangedFiresOncePerEdit_AndNotOnCursorMovement()
    {
        var area = new TextArea { Text = "ab\ncd" };
        var changes = 0;
        area.TextChanged += _ => changes++;

        area.OnKey(Key(ConsoleKey.UpArrow));
        area.OnKey(Key(ConsoleKey.End));
        Assert.Equal(0, changes);

        area.OnKey(Key(ConsoleKey.X, 'x'));
        Assert.Equal(1, changes);

        area.Insert("1\n2\n3");
        Assert.Equal(2, changes);
    }

    [Fact]
    public void RendersLinesTopDown()
    {
        var area = new TextArea { Text = "one\ntwo" };
        var rows = Render(area, 10, 3).ToText().Split('\n');

        Assert.StartsWith("one", rows[0]);
        Assert.StartsWith("two", rows[1]);
    }

    [Fact]
    public void VerticalScrollFollowsTheCursor()
    {
        var area = new TextArea { Text = "l1\nl2\nl3\nl4\nl5" }; // cursor ends on l5
        var rows = Render(area, 10, 2).ToText().Split('\n');

        Assert.StartsWith("l4", rows[0]);
        Assert.StartsWith("l5", rows[1]);
    }

    [Fact]
    public void HorizontalScrollKeepsTheCaretVisible()
    {
        var area = new TextArea { Text = "abcdefghij" }; // cursor at column 10
        var text = Render(area, 6, 1).ToText();

        Assert.Contains("fghij", text);
        Assert.DoesNotContain("abc", text);
    }

    [Fact]
    public void ThePlaceholderShowsOnlyWhileEmptyAndUnfocused()
    {
        var area = new TextArea { Placeholder = "hint" };

        Assert.Contains("hint", Render(area, 10, 3, focus: false).ToText());
        Assert.DoesNotContain("hint", Render(new TextArea { Placeholder = "hint" }, 10, 3).ToText());
    }

    [Fact]
    public void TheCaretReversesTheCellOnlyWhenFocused()
    {
        var focused = new TextArea { Text = "ab" };
        var buffer = Render(focused, 10, 3);
        Assert.True(buffer[2, 0].Style.HasFlag(CellStyle.Reverse)); // behind the line's end

        var unfocused = new TextArea { Text = "ab" };
        buffer = Render(unfocused, 10, 3, focus: false);
        Assert.False(buffer[2, 0].Style.HasFlag(CellStyle.Reverse));
    }

    [Fact]
    public void ATopAndBottomBorderLeavesTheContentRowsBetweenTheLines()
    {
        var area = new TextArea { Text = "inhalt", BorderMode = BorderMode.TopAndBottom };
        var rows = Render(area, 10, 4).ToText().Split('\n');

        Assert.StartsWith("──────────", rows[0]);
        Assert.StartsWith("inhalt", rows[1]);
        Assert.StartsWith("──────────", rows[3]);
    }

    [Fact]
    public void ReplacingTheTextWithAShorterDocumentClampsCursorAndScroll()
    {
        var area = new TextArea { Text = "l1\nl2\nl3\nl4\nl5" };
        Render(area, 10, 2); // scrolled down to the cursor

        area.Text = "kurz";
        var text = Render(area, 10, 2).ToText();

        Assert.Contains("kurz", text);
        Assert.Equal((0, 4), (area.CursorLine, area.CursorColumn));
    }

    private sealed class CountingHighlighter : ISyntaxHighlighter
    {
        public int Calls { get; private set; }

        public IReadOnlyList<IReadOnlyList<HighlightSpan>> Highlight(IEnumerable<string> lines)
        {
            Calls++;
            return lines.Select(_ => (IReadOnlyList<HighlightSpan>)Array.Empty<HighlightSpan>()).ToList();
        }
    }

    [Fact]
    public void TheHighlighterRunsOncePerEdit_NotOncePerFrame()
    {
        var counter = new CountingHighlighter();
        var area = new TextArea { Text = "ab", Highlighter = counter };
        var app = new ConsoleApp();
        area.Left = 0;
        area.Top = 0;
        area.Width = 10;
        area.Height = 3;
        app.Root.Add(area);

        app.RenderOffscreen(10, 3);
        app.RenderOffscreen(10, 3);
        app.RenderOffscreen(10, 3);
        Assert.Equal(1, counter.Calls);

        area.OnKey(Key(ConsoleKey.X, 'x'));
        app.RenderOffscreen(10, 3);
        app.RenderOffscreen(10, 3);
        Assert.Equal(2, counter.Calls);
    }

    [Fact]
    public void MarkdownStylingReachesTheCells()
    {
        var area = new TextArea { Highlighter = new MarkdownHighlighter(), Text = "# Titel" };
        var buffer = Render(area, 20, 3, focus: false);

        Assert.True(buffer[0, 0].Style.HasFlag(CellStyle.Dim));   // the # marker
        Assert.True(buffer[2, 0].Style.HasFlag(CellStyle.Bold));  // the heading text
    }

    [Fact]
    public void EditingAFenceRestylesTheLinesBelowIt()
    {
        var highlighter = new MarkdownHighlighter();
        var area = new TextArea { Highlighter = highlighter, Text = "code\ntext" };

        var buffer = Render(area, 10, 3, focus: false);
        Assert.NotEqual(highlighter.CodeColor, buffer[0, 0].Foreground);

        // Insert a fence line above: everything below becomes code.
        area.OnKey(Key(ConsoleKey.Home, modifiers: ConsoleModifiers.Control));
        area.Insert("```\n");

        buffer = Render(area, 10, 4, focus: false);
        Assert.Equal(highlighter.CodeColor, buffer[0, 1].Foreground);
        Assert.Equal(highlighter.CodeColor, buffer[0, 2].Foreground);
    }
}
