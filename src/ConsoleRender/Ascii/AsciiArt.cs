namespace ConsoleRender;

/// <summary>
/// Displays ASCII art — either plain text lines with a single color, or a colored
/// glyph grid produced by <see cref="AsciiImageConverter"/> (e.g. from a clipboard image).
/// </summary>
public class AsciiArt : Control
{
    private string[] _lines = Array.Empty<string>();
    private AsciiImage? _image;

    public Color Foreground { get; set; } = Color.Default;

    public AsciiArt() { }

    public AsciiArt(string multilineText) => SetText(multilineText);

    /// <summary>Sets plain text art. Lines are split on newlines.</summary>
    public void SetText(string multilineText)
    {
        Guard.Against.Null(multilineText);

        _lines = multilineText.Replace("\r", "").Split('\n');
        _image = null;
    }

    /// <summary>Sets a colored glyph grid (per-cell colors).</summary>
    public void SetImage(AsciiImage image)
    {
        Guard.Against.Null(image);

        _image = image;
        _lines = Array.Empty<string>();
    }

    protected override Size GetPreferredSize(Size available)
    {
        if (_image is { } img) return new Size(img.Width, img.Height);
        return new Size(_lines.Length == 0 ? 0 : _lines.Max(l => l.Length), _lines.Length);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (_image is { } img)
        {
            int h = Math.Min(img.Height, Bounds.Height);
            int w = Math.Min(img.Width, Bounds.Width);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var (ch, color) = img[x, y];
                    buffer.Set(Bounds.X + x, Bounds.Y + y, ch, color);
                }
            return;
        }

        for (int y = 0; y < _lines.Length && y < Bounds.Height; y++)
            buffer.Write(Bounds.X, Bounds.Y + y, _lines[y], Foreground);
    }
}
