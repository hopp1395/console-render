namespace ConsoleRender.Tests;

public class ProgressBarTests
{
    private static ConsoleBuffer Render(ProgressBar bar, int width)
    {
        var app = new ConsoleApp();
        bar.Left = 0;
        bar.Top = 0;
        bar.Width = width;
        bar.Height = 1;
        app.Root.Add(bar);
        return app.RenderOffscreen(width, 1);
    }

    [Fact]
    public void FractionMapsTheValueIntoTheRange()
    {
        var bar = new ProgressBar { Minimum = 200, Maximum = 400, Value = 250 };

        Assert.Equal(0.25, bar.Fraction);
    }

    [Fact]
    public void FractionClampsValuesOutsideTheRange()
    {
        var bar = new ProgressBar { Value = 150 };
        Assert.Equal(1, bar.Fraction);

        bar.Value = -10;
        Assert.Equal(0, bar.Fraction);
    }

    [Fact]
    public void AnEmptyRangeCountsAsNoProgress_InsteadOfDividingByZero()
    {
        var bar = new ProgressBar { Minimum = 10, Maximum = 10, Value = 10 };

        Assert.Equal(0, bar.Fraction);
    }

    [Fact]
    public void TheFilledPartUsesTheBarColorAsBackground()
    {
        var bar = new ProgressBar { Value = 50, ShowPercent = false };
        var buffer = Render(bar, 10);

        for (int x = 0; x < 5; x++)
            Assert.Equal(bar.BarColor, buffer[x, 0].Background);
        for (int x = 5; x < 10; x++)
            Assert.Equal(bar.TrackColor, buffer[x, 0].Background);
    }

    [Fact]
    public void ABoundaryBetweenCellsGetsAPartialBlockGlyph()
    {
        // 45 % of 10 cells = 4.5 cells: four full cells, then a half block.
        var bar = new ProgressBar { Value = 45, ShowPercent = false };
        var buffer = Render(bar, 10);

        Assert.Equal('▌', buffer[4, 0].Char);
        Assert.Equal(bar.BarColor, buffer[4, 0].Foreground);
    }

    [Fact]
    public void ThePercentageIsWrittenOverTheBar()
    {
        var bar = new ProgressBar { Value = 42 };

        string text = Render(bar, 20).ToText();

        Assert.Contains("42 %", text);
    }

    [Fact]
    public void IndeterminateSweepsASegmentAcrossTheTrack()
    {
        var bar = new ProgressBar { Indeterminate = true };
        bar.Update(TimeSpan.FromSeconds(0.5));

        var buffer = Render(bar, 20);
        int litCells = 0;
        for (int x = 0; x < 20; x++)
            if (buffer[x, 0].Background == bar.BarColor)
                litCells++;

        Assert.InRange(litCells, 1, 5);
    }
}
