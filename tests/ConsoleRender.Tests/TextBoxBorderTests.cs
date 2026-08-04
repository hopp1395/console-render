namespace ConsoleRender.Tests;

public class TextBoxBorderTests
{
    private static ConsoleBuffer Render(Control control, int width, int height)
    {
        using var app = new ConsoleApp();
        app.Root.Add(control);
        return app.RenderOffscreen(width, height);
    }

    private static readonly ConsoleKeyInfo HomeKey = new('\0', ConsoleKey.Home, false, false, false);

    private static TextBox Box(BorderMode mode, string text) => Box(mode, text, 10, 3);

    private static TextBox Box(BorderMode mode, string text, int width, int height)
    {
        var box = RawBox(mode, text, width, height);
        // SetText leaves the caret at the end, which scrolls the view. These tests are about
        // the usable width, so park the caret at the start.
        box.OnKey(HomeKey);
        return box;
    }

    private static TextBox RawBox(BorderMode mode, string text, int width, int height)
    {
        var box = new TextBox
        {
            Left = 0, Top = 0, Width = width, Height = height,
            BorderMode = mode,
            Border = BorderStyle.Ascii,
        };
        box.SetText(text);
        return box;
    }

    [Fact]
    public void WithoutABorder_TheTextOccupiesTheTopRow()
    {
        Assert.Equal("abc       ", Render(Box(BorderMode.None, "abc", 10, 1), 10, 1).ToText());
    }

    [Fact]
    public void FullBorder_DrawsAClosedBoxWithTheTextInside()
    {
        Assert.Equal(
            "+--------+\n" +
            "|abc     |\n" +
            "+--------+",
            Render(Box(BorderMode.Full, "abc"), 10, 3).ToText());
    }

    [Fact]
    public void TopAndBottomBorder_LeavesTheSidesOpen()
    {
        Assert.Equal(
            "----------\n" +
            "abc       \n" +
            "----------",
            Render(Box(BorderMode.TopAndBottom, "abc"), 10, 3).ToText());
    }

    [Fact]
    public void FullBorder_ShortensTheUsableTextWidth()
    {
        // Eight cells of text fit between the side characters of a ten-cell box.
        Assert.Equal(
            "+--------+\n" +
            "|abcdefgh|\n" +
            "+--------+",
            Render(Box(BorderMode.Full, "abcdefghij"), 10, 3).ToText());
    }

    [Fact]
    public void TopAndBottomBorder_KeepsTheFullTextWidth()
    {
        Assert.Equal(
            "----------\n" +
            "abcdefghij\n" +
            "----------",
            Render(Box(BorderMode.TopAndBottom, "abcdefghij"), 10, 3).ToText());
    }

    [Fact]
    public void TooShortForItsBorder_TheTextWinsAndTheBorderIsDropped()
    {
        Assert.Equal("abc       ", Render(Box(BorderMode.Full, "abc", 10, 1), 10, 1).ToText());
    }

    [Fact]
    public void TooNarrowForAClosedBorder_TheTextWinsAsWell()
    {
        // A closed border needs three columns; with two there is no room for the sides.
        Assert.Equal(
            "ab\n" +
            "  \n" +
            "  ",
            Render(Box(BorderMode.Full, "ab", 2, 3), 2, 3).ToText());
    }

    [Fact]
    public void ABorderedFieldScrollsWithinItsNarrowerInnerWidth()
    {
        // With the caret at the end the field scrolls to keep it visible, and the closed
        // border makes that window two cells narrower than the control.
        var box = RawBox(BorderMode.Full, "abcdefghij", 10, 3);

        Assert.Equal(
            "+--------+\n" +
            "|defghij |\n" +
            "+--------+",
            Render(box, 10, 3).ToText());
    }

    [Theory]
    [InlineData(BorderMode.None, 20, 1)]
    [InlineData(BorderMode.Full, 22, 3)]
    [InlineData(BorderMode.TopAndBottom, 20, 3)]
    public void ThePreferredSizeAccountsForTheBorder(BorderMode mode, int expectedWidth, int expectedHeight)
    {
        var box = new TextBox { Left = 0, Top = 0, BorderMode = mode };

        box.PerformLayout(new Rect(0, 0, 100, 40));

        Assert.Equal(expectedWidth, box.Bounds.Width);
        Assert.Equal(expectedHeight, box.Bounds.Height);
    }

    [Fact]
    public void TheBorderStyleIsConfigurable()
    {
        var box = new TextBox
        {
            Left = 0, Top = 0, Width = 6, Height = 3,
            BorderMode = BorderMode.Full,
            Border = BorderStyle.Double,
        };

        string text = Render(box, 6, 3).ToText();

        Assert.StartsWith("╔════╗", text);
    }

    [Fact]
    public void CommandInput_InheritsTheBorderOption()
    {
        var input = new CommandInput
        {
            Left = 0, Top = 0, Width = 8, Height = 3,
            BorderMode = BorderMode.TopAndBottom,
            Border = BorderStyle.Ascii,
            Placeholder = "",
        };

        string text = Render(input, 8, 3).ToText();

        Assert.StartsWith("--------", text);
        Assert.EndsWith("--------", text);
    }

    [Fact]
    public void Border_RejectsNull()
    {
        var box = new TextBox();

        Assert.Throws<ArgumentNullException>(() => box.Border = null!);
    }
}
