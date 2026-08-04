namespace ConsoleRender.Tests;

public class RendererTests
{
    private static string Present(Renderer renderer)
    {
        var writer = new StringWriter();
        renderer.Present(writer);
        return writer.ToString();
    }

    [Fact]
    public void SecondPresent_WithoutChanges_WritesNothing()
    {
        var renderer = new Renderer(20, 3);
        renderer.Buffer.Write(0, 0, "hallo");

        Assert.NotEqual("", Present(renderer));
        Assert.Equal("", Present(renderer));
    }

    [Fact]
    public void Present_EmitsOnlyChangedCells()
    {
        var renderer = new Renderer(20, 3);
        renderer.Buffer.Write(0, 0, "hallo");
        Present(renderer);

        renderer.Buffer.Set(2, 0, 'X');
        string output = Present(renderer);

        // One cursor move to row 1 / column 3, then the single changed character.
        Assert.Contains("\x1b[1;3H", output);
        Assert.Contains("X", output);
        Assert.DoesNotContain("hallo", output);
    }

    [Fact]
    public void Invalidate_ForcesAFullRepaint()
    {
        var renderer = new Renderer(10, 1);
        renderer.Buffer.Write(0, 0, "abc");
        Present(renderer);

        renderer.Invalidate();

        Assert.Contains("abc", Present(renderer));
    }

    [Fact]
    public void Present_EmitsTrueColorSequencesForRgbColors()
    {
        var renderer = new Renderer(4, 1);
        renderer.Buffer.Set(0, 0, 'A', Color.Rgb(10, 20, 30), Color.Rgb(40, 50, 60));

        string output = Present(renderer);

        Assert.Contains("38;2;10;20;30", output);
        Assert.Contains("48;2;40;50;60", output);
    }

    [Fact]
    public void Present_UsesDefaultColorCodesForTheDefaultColor()
    {
        var renderer = new Renderer(4, 1);
        renderer.Buffer.Set(0, 0, 'A');

        string output = Present(renderer);

        Assert.Contains(";39", output);
        Assert.Contains(";49", output);
    }

    [Fact]
    public void Resize_RepaintsEverythingOnTheNextPresent()
    {
        var renderer = new Renderer(10, 2);
        renderer.Buffer.Write(0, 0, "abc");
        Present(renderer);

        renderer.Resize(12, 3);
        renderer.Buffer.Write(0, 0, "abc");

        Assert.Equal(12, renderer.Width);
        Assert.Equal(3, renderer.Height);
        Assert.Contains("abc", Present(renderer));
    }

    [Fact]
    public void Resize_ToTheSameSize_KeepsTheFrontBuffer()
    {
        var renderer = new Renderer(10, 2);
        renderer.Buffer.Write(0, 0, "abc");
        Present(renderer);

        renderer.Resize(10, 2);

        Assert.Equal("", Present(renderer));
    }

    [Fact]
    public void Present_RejectsNullWriter()
    {
        var renderer = new Renderer(4, 1);

        Assert.Throws<ArgumentNullException>(() => renderer.Present(null!));
    }
}
