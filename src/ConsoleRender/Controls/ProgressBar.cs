namespace ConsoleRender;

/// <summary>
/// A horizontal progress bar. In the determinate form it fills according to
/// <see cref="Value"/> within <see cref="Minimum"/>..<see cref="Maximum"/>, with sub-cell
/// precision via partial block glyphs. With <see cref="Indeterminate"/> a segment sweeps
/// across the track instead, for work of unknown length.
///
/// The bar is drawn with background colors, so the percentage text can sit on top of it.
/// </summary>
public class ProgressBar : Control
{
    /// <summary>Left-eighth block glyphs, one per additional eighth of a cell.</summary>
    private const string PartialBlocks = "▏▎▍▌▋▊▉";

    /// <summary>Cells per second the indeterminate segment travels.</summary>
    private const double SweepSpeed = 24;

    private double minimum;
    private double maximum = 100;
    private double value;
    private double elapsed;

    public double Minimum
    {
        get => minimum;
        set => minimum = Guard.Against.InvalidInput(value, nameof(value), v => !double.IsNaN(v));
    }

    public double Maximum
    {
        get => maximum;
        set => maximum = Guard.Against.InvalidInput(value, nameof(value), v => !double.IsNaN(v));
    }

    /// <summary>Current progress. Values outside the range are shown clamped.</summary>
    public double Value
    {
        get => value;
        set => this.value = Guard.Against.InvalidInput(value, nameof(value), v => !double.IsNaN(v));
    }

    /// <summary>Sweeps a segment instead of filling — for work of unknown length.</summary>
    public bool Indeterminate { get; set; }

    /// <summary>Whether the percentage is written over the bar. Ignored while indeterminate.</summary>
    public bool ShowPercent { get; set; } = true;

    public Color BarColor { get; set; } = Color.Green;
    public Color TrackColor { get; set; } = Color.Rgb(50, 50, 58);
    public Color TextColor { get; set; } = Color.White;

    /// <summary>The filled fraction, clamped to 0..1. Zero when the range is empty.</summary>
    public double Fraction => maximum <= minimum
        ? 0
        : Math.Clamp((value - minimum) / (maximum - minimum), 0, 1);

    protected override Size GetPreferredSize(Size available) => new(20, 1);

    public override void Update(TimeSpan delta)
    {
        Guard.Against.Negative(delta);

        if (Indeterminate)
            elapsed += delta.TotalSeconds;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        int width = Bounds.Width;
        if (width < 1 || Bounds.Height < 1) return;
        int y = Bounds.Y + (Bounds.Height - 1) / 2;

        if (Indeterminate)
        {
            DrawSweep(buffer, width, y);
            return;
        }

        // Full cells are colored background; the boundary cell carries a partial block glyph.
        double filled = Fraction * width;
        int full = (int)filled;
        int eighths = (int)((filled - full) * 8);

        for (int i = 0; i < width; i++)
            buffer.Set(Bounds.X + i, y, ' ', TextColor, i < full ? BarColor : TrackColor);
        if (eighths > 0 && full < width)
            buffer.Set(Bounds.X + full, y, PartialBlocks[eighths - 1], BarColor, TrackColor);

        if (!ShowPercent) return;

        string text = $"{(int)Math.Round(Fraction * 100)} %";
        int start = Math.Max(0, (width - text.Length) / 2);
        for (int i = 0; i < text.Length && start + i < width; i++)
            buffer.Set(Bounds.X + start + i, y, text[i], TextColor,
                start + i < full ? BarColor : TrackColor);
    }

    private void DrawSweep(ConsoleBuffer buffer, int width, int y)
    {
        int segment = Math.Max(3, width / 4);
        // The segment enters from the left and leaves on the right before wrapping around.
        int position = (int)(elapsed * SweepSpeed % (width + segment)) - segment;

        for (int i = 0; i < width; i++)
        {
            bool inSegment = i >= position && i < position + segment;
            buffer.Set(Bounds.X + i, y, ' ', TextColor, inSegment ? BarColor : TrackColor);
        }
    }
}
