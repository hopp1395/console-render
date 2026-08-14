namespace ConsoleRender.Tests;

public class TabControlTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', ConsoleModifiers modifiers = 0)
    {
        return new(ch, key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));
    }

    [Fact]
    public void AddTab_TheFirstTabsContentIsVisibleAndLaterOnesAreHidden()
    {
        var tabs = new TabControl();
        var first = new Label("A");
        var second = new Label("B");

        tabs.AddTab("Erste", first);
        tabs.AddTab("Zweite", second);

        Assert.True(first.Visible);
        Assert.False(second.Visible);
    }

    [Fact]
    public void AddTab_StretchesTheContentToFillTheContentRect()
    {
        var tabs = new TabControl();
        var content = new Label("A");

        tabs.AddTab("Erste", content);

        Assert.Equal(0, content.Left);
        Assert.Equal(0, content.Top);
        Assert.Equal(0, content.Right);
        Assert.Equal(0, content.Bottom);
    }

    [Fact]
    public void AddTab_AddsTheContentAsAChild()
    {
        var tabs = new TabControl();

        tabs.AddTab("Erste", new Label("A"));
        tabs.AddTab("Zweite", new Label("B"));

        Assert.Equal(2, tabs.Children.Count);
        Assert.Equal(new[] { "Erste", "Zweite" }, tabs.Titles);
    }

    [Fact]
    public void AddTab_RejectsAnEmptyOrWhitespaceTitle()
    {
        var tabs = new TabControl();

        Assert.Throws<ArgumentException>(() => tabs.AddTab("", new Label("A")));
        Assert.Throws<ArgumentException>(() => tabs.AddTab("   ", new Label("A")));
    }

    [Fact]
    public void AddTab_RejectsANullContent()
    {
        var tabs = new TabControl();

        Assert.Throws<ArgumentNullException>(() => tabs.AddTab("Titel", null!));
    }

    [Fact]
    public void SelectedIndex_SwitchesWhichTabsContentIsVisible()
    {
        var tabs = new TabControl();
        var first = new Label("A");
        var second = new Label("B");
        tabs.AddTab("Erste", first);
        tabs.AddTab("Zweite", second);

        tabs.SelectedIndex = 1;

        Assert.False(first.Visible);
        Assert.True(second.Visible);
    }

    [Fact]
    public void SelectedIndex_IsANoOpWhenSetToTheCurrentValue()
    {
        var tabs = new TabControl();
        tabs.AddTab("Erste", new Label("A"));
        tabs.SelectionChanged += _ => Assert.Fail("should not fire");

        tabs.SelectedIndex = 0;
    }

    [Fact]
    public void SelectedIndex_OutOfRangeThrows()
    {
        var tabs = new TabControl();
        tabs.AddTab("Erste", new Label("A"));

        Assert.Throws<ArgumentOutOfRangeException>(() => tabs.SelectedIndex = 1);
    }

    [Fact]
    public void SelectedIndex_OnAnEmptyControlIsAlwaysZero()
    {
        var tabs = new TabControl();

        tabs.SelectedIndex = 0;

        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void SelectionChanged_FiresWithTheNewIndex_OnlyWhenItActuallyChanges()
    {
        var tabs = new TabControl();
        tabs.AddTab("Erste", new Label("A"));
        tabs.AddTab("Zweite", new Label("B"));
        var fired = new List<int>();
        tabs.SelectionChanged += fired.Add;

        tabs.SelectedIndex = 1;
        tabs.SelectedIndex = 1;

        Assert.Equal(new[] { 1 }, fired);
    }

    [Fact]
    public void ArrowKeysMoveTheSelectionAndWrapAround()
    {
        var tabs = new TabControl();
        tabs.AddTab("A", new Label("a"));
        tabs.AddTab("B", new Label("b"));
        tabs.AddTab("C", new Label("c"));

        tabs.OnKey(Key(ConsoleKey.LeftArrow));
        Assert.Equal(2, tabs.SelectedIndex);

        tabs.OnKey(Key(ConsoleKey.RightArrow));
        tabs.OnKey(Key(ConsoleKey.RightArrow));
        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void HomeAndEndJumpToTheFirstAndLastTab()
    {
        var tabs = new TabControl();
        tabs.AddTab("A", new Label("a"));
        tabs.AddTab("B", new Label("b"));
        tabs.AddTab("C", new Label("c"));

        tabs.OnKey(Key(ConsoleKey.End));
        Assert.Equal(2, tabs.SelectedIndex);

        tabs.OnKey(Key(ConsoleKey.Home));
        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void TabIsNotConsumed_SoFocusCyclingStillWorks()
    {
        var tabs = new TabControl();
        tabs.AddTab("A", new Label("a"));

        Assert.False(tabs.OnKey(Key(ConsoleKey.Tab, '\t')));
    }

    [Fact]
    public void TabControl_IsATabStopAndTabMovesFocusIntoTheActiveTabsContent()
    {
        using var app = new ConsoleApp();
        var tabControl = new TabControl { Left = 0, Top = 0, Width = 30, Height = 10 };
        var textBox = new TextBox { Left = 0, Top = 0 };
        tabControl.AddTab("Erste", textBox);
        app.Root.Add(tabControl);
        app.SetFocus(tabControl);

        app.CycleFocus();

        Assert.Same(textBox, app.FocusedControl);

        textBox.OnKey(Key(ConsoleKey.A, 'a'));
        Assert.Equal("a", textBox.Text);
    }

    [Fact]
    public void ShiftTabFromContentReturnsFocusToTheHeader()
    {
        using var app = new ConsoleApp();
        var tabControl = new TabControl { Left = 0, Top = 0, Width = 30, Height = 10 };
        var textBox = new TextBox { Left = 0, Top = 0 };
        tabControl.AddTab("Erste", textBox);
        app.Root.Add(tabControl);
        app.SetFocus(tabControl);
        app.CycleFocus();
        Assert.Same(textBox, app.FocusedControl);

        app.CycleFocus(backwards: true);

        Assert.Same(tabControl, app.FocusedControl);
    }

    [Fact]
    public void TabControl_SkipsInactiveTabsContentInTheFocusCycle()
    {
        using var app = new ConsoleApp();
        var tabControl = new TabControl { Left = 0, Top = 0, Width = 30, Height = 10 };
        var first = new TextBox { Left = 0, Top = 0 };
        var second = new TextBox { Left = 0, Top = 0 };
        tabControl.AddTab("Erste", first);
        tabControl.AddTab("Zweite", second);
        app.Root.Add(tabControl);
        app.SetFocus(tabControl);

        app.CycleFocus();
        Assert.Same(first, app.FocusedControl);

        // Cycling again should return to the header, not reach the hidden second tab's box.
        app.CycleFocus();
        Assert.Same(tabControl, app.FocusedControl);
    }

    [Fact]
    public void OnlyTheActiveTabsContentTextAppearsInTheRenderedOutput()
    {
        using var app = new ConsoleApp();
        var tabControl = new TabControl { Left = 0, Top = 0, Width = 30, Height = 10 };
        tabControl.AddTab("Erste", new Label("SichtbarerText") { Left = 0, Top = 0 });
        tabControl.AddTab("Zweite", new Label("VerstecktText") { Left = 0, Top = 0 });
        app.Root.Add(tabControl);

        string text = app.RenderOffscreen(30, 10).ToText();

        Assert.Contains("SichtbarerText", text);
        Assert.DoesNotContain("VerstecktText", text);
    }

    [Fact]
    public void TheHeaderRowShowsEveryTabTitle()
    {
        using var app = new ConsoleApp();
        var tabControl = new TabControl { Left = 0, Top = 0, Width = 30, Height = 10 };
        tabControl.AddTab("Übersicht", new Label("x") { Left = 0, Top = 0 });
        tabControl.AddTab("Formular", new Label("y") { Left = 0, Top = 0 });
        app.Root.Add(tabControl);

        string[] lines = app.RenderOffscreen(30, 10).ToText().Split('\n');

        Assert.Contains("Übersicht", lines[0]);
        Assert.Contains("Formular", lines[0]);
    }
}
