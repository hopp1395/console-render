namespace ConsoleRender;

/// <summary>
/// A transparent container that only groups and positions children. Useful as a layout
/// region without a visible border; set <see cref="Background"/> to fill it.
/// </summary>
public class Panel : Control
{
    private int _padding;

    public Color Background { get; set; } = Color.Default;

    /// <summary>Inner margin applied to the area given to children.</summary>
    public int Padding
    {
        get => _padding;
        set => _padding = Guard.Against.Negative(value);
    }

    public override Rect ContentRect => Padding > 0 ? Bounds.Deflate(Padding) : Bounds;

    protected override Size GetPreferredSize(Size available) => new(available.Width, available.Height);

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (!Background.IsDefault)
            buffer.FillRect(Bounds, ' ', Color.Default, Background);
    }
}
