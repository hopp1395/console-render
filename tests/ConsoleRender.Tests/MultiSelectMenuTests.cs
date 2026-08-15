namespace ConsoleRender.Tests;

public class MultiSelectMenuTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0')
    {
        return new(ch, key, false, false, false);
    }

    [Fact]
    public void MovingTheCursorAloneChecksNothing()
    {
        var menu = new MultiSelectMenu("a", "b", "c");

        menu.OnKey(Key(ConsoleKey.DownArrow));

        Assert.Empty(menu.CheckedIndices);
    }

    [Fact]
    public void SpaceTogglesTheHighlightedItem()
    {
        var menu = new MultiSelectMenu("a", "b", "c");

        menu.OnKey(Key(ConsoleKey.DownArrow));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Equal(new[] { 1 }, menu.CheckedIndices);

        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Empty(menu.CheckedIndices);
    }

    [Fact]
    public void SpaceCanCheckSeveralItems()
    {
        var menu = new MultiSelectMenu("a", "b", "c");

        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));
        menu.OnKey(Key(ConsoleKey.DownArrow));
        menu.OnKey(Key(ConsoleKey.DownArrow));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Equal(new HashSet<int> { 0, 2 }, menu.CheckedIndices);
    }

    [Fact]
    public void ItemCheckedChanged_FiresWithTheIndexAndNewState()
    {
        var menu = new MultiSelectMenu("a", "b");
        var raised = new List<(int Index, bool Checked)>();
        menu.ItemCheckedChanged += (index, isChecked) => raised.Add((index, isChecked));

        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Equal(new[] { (0, true), (0, false) }, raised);
    }

    [Fact]
    public void Submitted_FiresWithTheCheckedIndicesOnEnter()
    {
        var menu = new MultiSelectMenu("a", "b", "c");
        IReadOnlyCollection<int>? submitted = null;
        menu.Submitted += indices => submitted = indices;

        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));
        menu.OnKey(Key(ConsoleKey.End));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));
        menu.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal(new[] { 0, 2 }, submitted?.OrderBy(i => i));
    }

    [Fact]
    public void ArrowKeysWrapAround()
    {
        var menu = new MultiSelectMenu("a", "b", "c");

        menu.OnKey(Key(ConsoleKey.UpArrow));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Equal(new[] { 2 }, menu.CheckedIndices);
    }

    [Fact]
    public void HomeAndEndJumpToTheFirstAndLastItem()
    {
        var menu = new MultiSelectMenu("a", "b", "c");

        menu.OnKey(Key(ConsoleKey.End));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));
        menu.OnKey(Key(ConsoleKey.Home));
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Equal(new HashSet<int> { 0, 2 }, menu.CheckedIndices);
    }

    [Fact]
    public void EmptyMenu_IgnoresKeys()
    {
        var menu = new MultiSelectMenu();

        Assert.False(menu.OnKey(Key(ConsoleKey.DownArrow)));
        Assert.False(menu.OnKey(Key(ConsoleKey.Spacebar, ' ')));
    }

    [Fact]
    public void RendersCheckedAndUncheckedItemsWithTheirBoxGlyph()
    {
        var menu = new MultiSelectMenu("Eins", "Zwei");
        menu.OnKey(Key(ConsoleKey.Spacebar, ' '));
        menu.Left = 0;
        menu.Top = 0;
        menu.Width = 10;
        menu.Height = 2;

        using var app = new ConsoleApp();
        app.Root.Add(menu);

        var lines = app.RenderOffscreen(10, 2).ToText().Split('\n');

        Assert.StartsWith("[x] Eins", lines[0]);
        Assert.StartsWith("[ ] Zwei", lines[1]);
    }

    [Fact]
    public void ScrollingKeepsTheCursorInView()
    {
        var menu = new MultiSelectMenu();
        for (var i = 0; i < 10; i++)
        {
            menu.Items.Add(i.ToString());
        }

        menu.Left = 0;
        menu.Top = 0;
        menu.Width = 6;
        menu.Height = 3;

        using var app = new ConsoleApp();
        app.Root.Add(menu);
        app.SetFocus(menu);

        for (var i = 0; i < 9; i++)
        {
            menu.OnKey(Key(ConsoleKey.DownArrow));
        }

        var text = app.RenderOffscreen(6, 3).ToText();

        Assert.Contains("9", text);
        Assert.DoesNotContain("[ ] 0", text);
    }
}
