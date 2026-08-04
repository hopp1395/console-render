namespace ConsoleRender;

/// <summary>Built-in text animations for <see cref="Label"/>.</summary>
public enum TextEffect
{
    None,
    /// <summary>Text toggles visibility twice per second.</summary>
    Blink,
    /// <summary>Animated hue gradient across the characters.</summary>
    Rainbow,
    /// <summary>Brightness oscillates smoothly.</summary>
    Pulse,
}

/// <summary>A single-line text output field with color, style and optional animation.</summary>
public class Label : Control
{
    private double _elapsed;
    private string _text = "";

    public string Text
    {
        get => _text;
        set => _text = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;
    public CellStyle Style { get; set; } = CellStyle.None;
    public TextEffect Effect { get; set; } = TextEffect.None;

    /// <summary>Alignment of the text inside the control's own bounds.</summary>
    public TextAlignment TextAlign { get; set; } = TextAlignment.Left;

    public Label() { }

    public Label(string text) => Text = Guard.Against.Null(text);

    protected override Size GetPreferredSize(Size available) => new(Text.Length, 1);

    public override void Update(TimeSpan delta)
    {
        Guard.Against.Negative(delta);

        _elapsed += delta.TotalSeconds;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1) return;

        string text = Text.Length > Bounds.Width ? Text[..Bounds.Width] : Text;
        int x = TextAlign switch
        {
            TextAlignment.Center => Bounds.X + (Bounds.Width - text.Length) / 2,
            TextAlignment.Right => Bounds.Right - text.Length,
            _ => Bounds.X,
        };
        int y = Bounds.Y;

        if (!Background.IsDefault)
            buffer.FillRect(new Rect(Bounds.X, y, Bounds.Width, 1), ' ', Foreground, Background);

        switch (Effect)
        {
            case TextEffect.Blink:
                if (_elapsed % 1.0 < 0.5)
                    buffer.Write(x, y, text, Foreground, Background, Style);
                break;

            case TextEffect.Rainbow:
                for (int i = 0; i < text.Length; i++)
                {
                    var c = Color.FromHsv(i * 18 + _elapsed * 140, 0.85, 1);
                    buffer.Set(x + i, y, text[i], c, Background, Style);
                }
                break;

            case TextEffect.Pulse:
                double v = 0.55 + 0.45 * Math.Sin(_elapsed * 4);
                var fg = Foreground.IsDefault ? Color.White : Foreground;
                buffer.Write(x, y, text, fg.Scale(v), Background, Style);
                break;

            default:
                buffer.Write(x, y, text, Foreground, Background, Style);
                break;
        }
    }
}
