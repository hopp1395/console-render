namespace ConsoleRender.Tests;

public class TableTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0')
    {
        return new(ch, key, false, false, false);
    }

    private static Table Cities()
    {
        var table = new Table();
        table.AddColumn("Stadt", 12);
        table.AddColumn("Einwohner", 10);
        table.AddRow("Berlin", "3.7M");
        table.AddRow("Hamburg", "1.9M");
        table.AddRow("München", "1.5M");
        return table;
    }

    [Fact]
    public void AddColumn_RejectsAnEmptyOrWhitespaceHeader()
    {
        var table = new Table();

        Assert.Throws<ArgumentException>(() => table.AddColumn("", 10));
        Assert.Throws<ArgumentException>(() => table.AddColumn("  ", 10));
    }

    [Fact]
    public void AddColumn_RejectsAZeroOrNegativeWidth()
    {
        var table = new Table();

        Assert.Throws<ArgumentException>(() => table.AddColumn("x", 0));
        Assert.Throws<ArgumentException>(() => table.AddColumn("x", -1));
    }

    [Fact]
    public void AddRow_RejectsAWrongCellCount()
    {
        var table = new Table();
        table.AddColumn("A", 5);
        table.AddColumn("B", 5);

        Assert.Throws<ArgumentException>(() => table.AddRow("only one"));
    }

    [Fact]
    public void SelectedIndex_DefaultsToTheFirstRow()
    {
        var table = Cities();

        Assert.Equal(0, table.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex_OutOfRangeThrows()
    {
        var table = Cities();

        Assert.Throws<ArgumentOutOfRangeException>(() => table.SelectedIndex = 99);
    }

    [Fact]
    public void SelectedIndex_OnAnEmptyTableIsAlwaysZero()
    {
        var table = new Table();
        table.AddColumn("A", 5);

        table.SelectedIndex = 0;

        Assert.Equal(0, table.SelectedIndex);
    }

    [Fact]
    public void ArrowKeysMoveTheSelectionAndClampAtTheEnds()
    {
        var table = Cities();

        table.OnKey(Key(ConsoleKey.UpArrow));
        Assert.Equal(0, table.SelectedIndex);

        table.OnKey(Key(ConsoleKey.DownArrow));
        table.OnKey(Key(ConsoleKey.DownArrow));
        table.OnKey(Key(ConsoleKey.DownArrow));
        Assert.Equal(2, table.SelectedIndex);
    }

    [Fact]
    public void HomeAndEndJumpToTheFirstAndLastRow()
    {
        var table = Cities();

        table.OnKey(Key(ConsoleKey.End));
        Assert.Equal(2, table.SelectedIndex);

        table.OnKey(Key(ConsoleKey.Home));
        Assert.Equal(0, table.SelectedIndex);
    }

    [Fact]
    public void SelectionChanged_FiresWithTheNewIndex_OnlyWhenItActuallyChanges()
    {
        var table = Cities();
        var fired = new List<int>();
        table.SelectionChanged += fired.Add;

        table.OnKey(Key(ConsoleKey.UpArrow)); // already at 0, clamps to 0, no change
        table.OnKey(Key(ConsoleKey.DownArrow));

        Assert.Equal(new[] { 1 }, fired);
    }

    [Fact]
    public void RowActivated_FiresWithTheSelectedIndexOnEnter()
    {
        var table = Cities();
        int? activated = null;
        table.RowActivated += index => activated = index;

        table.OnKey(Key(ConsoleKey.DownArrow));
        table.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal(1, activated);
    }

    [Fact]
    public void RendersTheHeaderAndRows()
    {
        var table = Cities();
        table.Left = 0;
        table.Top = 0;
        table.Width = 24;
        table.Height = 4;

        using var app = new ConsoleApp();
        app.Root.Add(table);

        string[] lines = app.RenderOffscreen(24, 4).ToText().Split('\n');

        Assert.Contains("Stadt", lines[0]);
        Assert.Contains("Einwohner", lines[0]);
        Assert.Contains("Berlin", lines[1]);
        Assert.Contains("Hamburg", lines[2]);
        Assert.Contains("München", lines[3]);
    }

    [Fact]
    public void LongCellTextIsTruncatedToItsColumnWidth_NotOverflowingIntoTheNextColumn()
    {
        var table = new Table();
        table.AddColumn("Kurz", 5);
        table.AddColumn("Zweite", 6);
        table.AddRow("VielZuLangerText", "X");
        table.Left = 0;
        table.Top = 0;
        table.Width = 20;
        table.Height = 2;

        using var app = new ConsoleApp();
        app.Root.Add(table);

        string[] lines = app.RenderOffscreen(20, 2).ToText().Split('\n');

        // The overlong first cell must not swallow the separator or the second column's "X".
        Assert.Equal('│', lines[1][5]);
        Assert.Equal('X', lines[1][6]);
    }

    [Fact]
    public void AnOverflowingCellInTheSelectedRowScrollsOverTime()
    {
        var table = new Table();
        table.AddColumn("N", 5);
        table.AddRow("ABCDEFGHIJ");
        table.Left = 0;
        table.Top = 0;
        table.Width = 5;
        table.Height = 2;

        using var app = new ConsoleApp();
        app.Root.Add(table);

        string before = app.RenderOffscreen(5, 2).ToText().Split('\n')[1];
        Assert.Equal("ABCDE", before);

        table.Update(TimeSpan.FromSeconds(1)); // 3 chars/second

        string after = app.RenderOffscreen(5, 2).ToText().Split('\n')[1];
        Assert.Equal("DEFGH", after);
    }

    [Fact]
    public void ScrollingIsInactiveOnRowsThatAreNotSelected()
    {
        var table = new Table();
        table.AddColumn("N", 5);
        table.AddRow("AB"); // selected by default, too short to overflow
        table.AddRow("ABCDEFGHIJ"); // not selected, overflows

        string text = RenderText(table, 5, 3);

        table.Update(TimeSpan.FromSeconds(5));
        string afterWaiting = RenderText(table, 5, 3);

        // The non-selected overflowing row stays statically truncated, unaffected by time.
        string unselectedRow = afterWaiting.Split('\n')[2];
        Assert.Equal("ABCDE", unselectedRow);
        Assert.Equal(text.Split('\n')[2], unselectedRow);
    }

    [Fact]
    public void ShortCellsInTheSelectedRowDoNotScroll()
    {
        var table = Cities();
        table.Update(TimeSpan.FromSeconds(10));

        string firstLine = RenderText(table, 24, 4).Split('\n')[1];

        Assert.StartsWith("Berlin", firstLine);
    }

    private static string RenderText(Table table, int width, int height)
    {
        table.Parent?.Remove(table);
        table.Left = 0;
        table.Top = 0;
        table.Width = width;
        table.Height = height;

        using var app = new ConsoleApp();
        app.Root.Add(table);
        return app.RenderOffscreen(width, height).ToText();
    }

    [Fact]
    public void ScrollingKeepsTheSelectionInView()
    {
        var table = new Table();
        table.AddColumn("N", 4);
        for (int i = 0; i < 10; i++)
            table.AddRow(i.ToString());
        table.Left = 0;
        table.Top = 0;
        table.Width = 6;
        table.Height = 4; // header + 3 visible rows

        using var app = new ConsoleApp();
        app.Root.Add(table);
        app.SetFocus(table);

        for (int i = 0; i < 9; i++)
            table.OnKey(Key(ConsoleKey.DownArrow));

        string text = app.RenderOffscreen(6, 4).ToText();

        Assert.Contains("9", text);
        Assert.DoesNotContain("0   ", text);
    }
}
