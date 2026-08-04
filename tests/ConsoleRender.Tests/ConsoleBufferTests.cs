namespace ConsoleRender.Tests;

public class ConsoleBufferTests
{
    [Fact]
    public void Write_ClipsTextAtTheRightEdge()
    {
        var buffer = new ConsoleBuffer(5, 1);

        buffer.Write(3, 0, "abcdef");

        Assert.Equal("   ab", buffer.ToText());
    }

    [Fact]
    public void Write_SkipsCellsLeftOfTheOrigin()
    {
        var buffer = new ConsoleBuffer(5, 1);

        buffer.Write(-2, 0, "abcdef");

        Assert.Equal("cdef ", buffer.ToText());
    }

    [Fact]
    public void Write_OutsideVerticalBounds_IsIgnored()
    {
        var buffer = new ConsoleBuffer(5, 2);

        buffer.Write(0, 5, "abc");
        buffer.Write(0, -1, "abc");

        Assert.Equal("     \n     ", buffer.ToText());
    }

    [Fact]
    public void PushClip_RestrictsWrites()
    {
        var buffer = new ConsoleBuffer(6, 1);

        buffer.PushClip(new Rect(2, 0, 2, 1));
        buffer.Write(0, 0, "abcdef");
        buffer.PopClip();

        Assert.Equal("  cd  ", buffer.ToText());
    }

    [Fact]
    public void PushClip_IntersectsWithTheEnclosingClip()
    {
        var buffer = new ConsoleBuffer(10, 1);

        buffer.PushClip(new Rect(2, 0, 4, 1));   // cells 2..5
        buffer.PushClip(new Rect(4, 0, 4, 1));   // cells 4..7, intersected to 4..5
        buffer.Write(0, 0, "abcdefghij");
        buffer.PopClip();
        buffer.PopClip();

        Assert.Equal("    ef    ", buffer.ToText());
    }

    [Fact]
    public void PopClip_WithoutPush_Throws()
    {
        var buffer = new ConsoleBuffer(4, 1);

        Assert.Throws<InvalidOperationException>(buffer.PopClip);
    }

    [Fact]
    public void FillRect_ClipsToTheBuffer()
    {
        var buffer = new ConsoleBuffer(4, 2);

        buffer.FillRect(new Rect(-2, -2, 10, 10), '#');

        Assert.Equal("####\n####", buffer.ToText());
    }

    [Fact]
    public void DrawBorder_DrawsCornersAndTitle()
    {
        var buffer = new ConsoleBuffer(10, 3);

        buffer.DrawBorder(new Rect(0, 0, 10, 3), BorderStyle.Ascii, title: "Hi");

        Assert.Equal(
            "+- Hi ---+\n" +
            "|        |\n" +
            "+--------+",
            buffer.ToText());
    }

    [Fact]
    public void DrawBorder_TooSmallRect_DrawsNothing()
    {
        var buffer = new ConsoleBuffer(4, 1);

        buffer.DrawBorder(new Rect(0, 0, 4, 1), BorderStyle.Ascii);

        Assert.Equal("    ", buffer.ToText());
    }

    [Fact]
    public void Constructor_RejectsNonPositiveSize()
    {
        Assert.Throws<ArgumentException>(() => new ConsoleBuffer(0, 5));
        Assert.Throws<ArgumentException>(() => new ConsoleBuffer(5, -1));
    }

    [Fact]
    public void Write_RejectsNullText()
    {
        var buffer = new ConsoleBuffer(4, 1);

        Assert.Throws<ArgumentNullException>(() => buffer.Write(0, 0, null!));
    }
}
