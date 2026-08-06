namespace ConsoleRender.Tests;

public class SearchBoxTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', ConsoleModifiers modifiers = 0)
    {
        return new(ch, key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));
    }

    private static SearchBox Cities()
    {
        return new("Berlin", "Hamburg", "München", "Köln", "Frankfurt");
    }

    [Fact]
    public void AnEmptyQueryShowsEveryItem()
    {
        var box = Cities();

        Assert.Equal(new[] { "Berlin", "Hamburg", "München", "Köln", "Frankfurt" }, box.Matches);
    }

    [Fact]
    public void TypingFiltersCaseInsensitively()
    {
        var box = Cities();

        box.OnKey(Key(ConsoleKey.B, 'b'));

        Assert.Equal("b", box.Query);
        Assert.Equal(new[] { "Berlin", "Hamburg" }, box.Matches);
    }

    [Fact]
    public void ArrowKeysMoveTheHighlightAndWrapAround()
    {
        var box = Cities();

        box.OnKey(Key(ConsoleKey.DownArrow));
        Assert.Equal("Hamburg", box.SelectedItem);

        box.OnKey(Key(ConsoleKey.UpArrow));
        box.OnKey(Key(ConsoleKey.UpArrow));
        Assert.Equal("Frankfurt", box.SelectedItem);
    }

    [Fact]
    public void EnterActivatesTheHighlightedMatch_WithItsIndexIntoItems()
    {
        var box = Cities();
        (int Index, string Item)? activated = null;
        box.ItemActivated += (index, item) => activated = (index, item);

        box.OnKey(Key(ConsoleKey.M, 'm'));
        box.OnKey(Key(ConsoleKey.Oem1, 'ü'));
        box.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal((2, "München"), activated);
    }

    [Fact]
    public void EnterWithoutAMatchActivatesNothing()
    {
        var box = Cities();
        bool activated = false;
        box.ItemActivated += (_, _) => activated = true;

        box.OnKey(Key(ConsoleKey.X, 'x'));
        box.OnKey(Key(ConsoleKey.Enter));

        Assert.False(activated);
        Assert.Empty(box.Matches);
        Assert.Null(box.SelectedItem);
    }

    [Fact]
    public void TheHighlightStaysOnTheSameItemWhileItStillMatches()
    {
        var box = Cities();

        // Highlight Hamburg, then narrow the list down to it.
        box.OnKey(Key(ConsoleKey.DownArrow));
        box.OnKey(Key(ConsoleKey.H, 'h'));
        box.OnKey(Key(ConsoleKey.A, 'a'));

        Assert.Equal("Hamburg", box.SelectedItem);
    }

    [Fact]
    public void TheHighlightFallsBackToTheFirstMatchWhenItsItemDropsOut()
    {
        var box = Cities();

        box.OnKey(Key(ConsoleKey.DownArrow));   // Hamburg
        box.OnKey(Key(ConsoleKey.K, 'k'));      // matches Köln and Frankfurt, not Hamburg

        Assert.Equal("Köln", box.SelectedItem);
    }

    [Fact]
    public void EscapeClearsTheQuery()
    {
        var box = Cities();

        box.OnKey(Key(ConsoleKey.B, 'b'));
        Assert.True(box.OnKey(Key(ConsoleKey.Escape)));

        Assert.Equal("", box.Query);
        Assert.Equal(5, box.Matches.Count);
    }

    [Fact]
    public void EscapeOnAnEmptyQueryIsNotConsumed_SoADialogCouldStillClose()
    {
        var box = Cities();

        Assert.False(box.OnKey(Key(ConsoleKey.Escape)));
    }

    [Fact]
    public void TabIsNotConsumed_SoFocusCyclingStillWorks()
    {
        var box = Cities();

        Assert.False(box.OnKey(Key(ConsoleKey.Tab, '\t')));
    }

    [Fact]
    public void TheInnerInputStaysOutOfTheFocusCycle()
    {
        var box = Cities();

        Assert.True(box.Focusable);
        Assert.False(box.Input.Focusable);
    }

    [Fact]
    public void ACustomFilterReplacesTheSubstringMatch()
    {
        var box = Cities();
        box.Filter = (query, item) => item.StartsWith(query, StringComparison.OrdinalIgnoreCase);

        box.OnKey(Key(ConsoleKey.K, 'k'));

        Assert.Equal(new[] { "Köln" }, box.Matches);
    }

    [Fact]
    public void RendersInputOnTopAndMatchesBelow()
    {
        var app = new ConsoleApp();
        var box = Cities();
        box.Left = 0;
        box.Top = 0;
        box.Width = 20;
        box.Height = 4;
        app.Root.Add(box);
        app.SetFocus(box);
        box.OnKey(Key(ConsoleKey.N, 'n'));

        string[] lines = app.RenderOffscreen(20, 4).ToText().Split('\n');

        Assert.Contains("n", lines[0]);
        Assert.Contains("› Berlin", lines[1]);
        Assert.Contains("  München", lines[2]);
        Assert.Contains("  Köln", lines[3]);
    }
}
