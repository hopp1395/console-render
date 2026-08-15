namespace ConsoleRender.Tests;

public class LayoutTests
{
    /// <summary>A control with a fixed preferred size, so anchor behaviour is observable.</summary>
    private sealed class Box : Control
    {
        private readonly Size preferred;

        public Box(int width, int height)
        {
            preferred = new Size(width, height);
        }

        protected override Size GetPreferredSize(Size available)
        {
            return preferred;
        }

        protected override void Draw(ConsoleBuffer buffer)
        {
            buffer.FillRect(Bounds, '#');
        }
    }

    private static readonly Rect Area = new(0, 0, 100, 40);

    [Fact]
    public void LeftAnchor_PlacesControlRelativeToLeftEdge()
    {
        var box = new Box(10, 2) { Left = 5, Top = 3 };

        box.PerformLayout(Area);

        Assert.Equal(new Rect(5, 3, 10, 2), box.Bounds);
    }

    [Fact]
    public void RightAnchor_PlacesControlRelativeToRightEdge()
    {
        var box = new Box(10, 2) { Right = 5, Bottom = 3 };

        box.PerformLayout(Area);

        Assert.Equal(new Rect(85, 35, 10, 2), box.Bounds);
    }

    [Fact]
    public void BothHorizontalAnchors_StretchTheControl()
    {
        var box = new Box(10, 2) { Left = 5, Right = 15, Top = 0 };

        box.PerformLayout(Area);

        Assert.Equal(5, box.Bounds.X);
        Assert.Equal(80, box.Bounds.Width);
    }

    [Fact]
    public void BothVerticalAnchors_StretchTheControl()
    {
        var box = new Box(10, 2) { Top = 2, Bottom = 8, Left = 0 };

        box.PerformLayout(Area);

        Assert.Equal(2, box.Bounds.Y);
        Assert.Equal(30, box.Bounds.Height);
    }

    [Fact]
    public void ExplicitSize_OverridesStretchingAnchors()
    {
        var box = new Box(10, 2) { Left = 0, Right = 0, Width = 25, Top = 0 };

        box.PerformLayout(Area);

        Assert.Equal(25, box.Bounds.Width);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, 0)]
    [InlineData(HorizontalAlignment.Center, 45)]
    [InlineData(HorizontalAlignment.Right, 90)]
    public void WithoutHorizontalAnchors_AlignmentDecidesPosition(HorizontalAlignment alignment, int expectedX)
    {
        var box = new Box(10, 2) { HorizontalAlignment = alignment, Top = 0 };

        box.PerformLayout(Area);

        Assert.Equal(expectedX, box.Bounds.X);
    }

    [Theory]
    [InlineData(VerticalAlignment.Top, 0)]
    [InlineData(VerticalAlignment.Middle, 19)]
    [InlineData(VerticalAlignment.Bottom, 38)]
    public void WithoutVerticalAnchors_AlignmentDecidesPosition(VerticalAlignment alignment, int expectedY)
    {
        var box = new Box(10, 2) { VerticalAlignment = alignment, Left = 0 };

        box.PerformLayout(Area);

        Assert.Equal(expectedY, box.Bounds.Y);
    }

    [Fact]
    public void Children_AreLaidOutInsideTheParentsContentRect()
    {
        var frame = new Frame("t") { Left = 0, Top = 0, Width = 20, Height = 10 };
        var child = new Box(5, 1) { Left = 0, Top = 0 };
        frame.Add(child);

        frame.PerformLayout(Area);

        // The frame's border consumes one cell on each side.
        Assert.Equal(new Rect(1, 1, 5, 1), child.Bounds);
    }

    [Fact]
    public void ResizingTheArea_MovesRightAnchoredControls()
    {
        var box = new Box(10, 2) { Right = 0, Top = 0 };

        box.PerformLayout(new Rect(0, 0, 100, 40));
        var wide = box.Bounds.X;
        box.PerformLayout(new Rect(0, 0, 60, 40));

        Assert.Equal(90, wide);
        Assert.Equal(50, box.Bounds.X);
    }

    [Fact]
    public void Add_RejectsAControlThatAlreadyHasAParent()
    {
        var first = new Panel();
        var second = new Panel();
        var child = new Box(1, 1);
        first.Add(child);

        Assert.Throws<InvalidOperationException>(() => second.Add(child));
    }

    [Fact]
    public void Add_RejectsNull()
    {
        var panel = new Panel();

        Assert.Throws<ArgumentNullException>(() => panel.Add(null!));
    }
}
