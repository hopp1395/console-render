namespace ConsoleRender;

/// <summary>An animated activity indicator with an optional text label.</summary>
public class Spinner : Control
{
    private static readonly char[] DefaultFrames = { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };

    private double elapsed;
    private string text = "";
    private char[] frames = DefaultFrames;
    private double interval = 0.08;

    public string Text
    {
        get => text;
        set => text = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Cyan;

    public char[] Frames
    {
        get => frames;
        set
        {
            Guard.Against.NullOrEmpty(value);
            frames = value;
        }
    }

    /// <summary>Seconds per animation frame.</summary>
    public double Interval
    {
        get => interval;
        set => interval = Guard.Against.NegativeOrZero(value);
    }

    public bool Active { get; set; } = true;

    protected override Size GetPreferredSize(Size available)
    {
        return new(Text.Length + 2, 1);
    }

    public override void Update(TimeSpan delta)
    {
        Guard.Against.Negative(delta);

        if (Active) elapsed += delta.TotalSeconds;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        char frame = Active ? Frames[(int)(elapsed / Interval) % Frames.Length] : '✓';
        buffer.Set(Bounds.X, Bounds.Y, frame, Foreground, default, CellStyle.Bold);
        if (Text.Length > 0)
            buffer.Write(Bounds.X + 2, Bounds.Y, Text, Foreground);
    }
}
