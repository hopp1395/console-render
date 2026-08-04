namespace ConsoleRender;

/// <summary>
/// A bordered message box, typically shown as a modal dialog via
/// <see cref="ConsoleApp.ShowDialog"/> or <see cref="ConsoleApp.ShowInfo"/>.
/// Any of Enter/Escape/Space closes it.
/// </summary>
public class InfoBox : Control
{
    private string _title = "";
    private string _text = "";
    private BorderStyle _border = BorderStyle.Double;
    private int _maxTextWidth = 48;

    public string Title
    {
        get => _title;
        set => _title = Guard.Against.Null(value);
    }

    public string Text
    {
        get => _text;
        set => _text = Guard.Against.Null(value);
    }

    public BorderStyle Border
    {
        get => _border;
        set => _border = Guard.Against.Null(value);
    }

    public Color BorderColor { get; set; } = Color.Cyan;
    public Color Foreground { get; set; } = Color.White;
    public Color Background { get; set; } = Color.DarkBlue;

    /// <summary>Maximum text width before wrapping.</summary>
    public int MaxTextWidth
    {
        get => _maxTextWidth;
        set => _maxTextWidth = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>Raised when the box is closed.</summary>
    public event Action? Closed;

    public InfoBox()
    {
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Middle;
    }

    public void Close() => Closed?.Invoke();

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Escape or ConsoleKey.Spacebar)
        {
            Close();
            return true;
        }
        return false;
    }

    private IReadOnlyList<string> WrapText(int width)
    {
        Guard.Against.NegativeOrZero(width);

        var lines = new List<string>();
        foreach (string paragraph in Text.Replace("\r", "").Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add("");
                continue;
            }
            var current = "";
            foreach (string word in paragraph.Split(' '))
            {
                if (current.Length == 0)
                    current = word;
                else if (current.Length + 1 + word.Length <= width)
                    current += " " + word;
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }
            if (current.Length > 0)
                lines.Add(current);
        }
        return lines;
    }

    protected override Size GetPreferredSize(Size available)
    {
        int textWidth = Math.Min(MaxTextWidth, Math.Max(10, available.Width - 8));
        var lines = WrapText(textWidth);
        int contentWidth = Math.Max(Title.Length + 2, Math.Max(lines.Count == 0 ? 0 : lines.Max(l => l.Length), 12));
        // Top border, the text, one blank row, and the bottom border that carries the hint.
        return new Size(contentWidth + 4, lines.Count + 3);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        buffer.FillRect(Bounds, ' ', Foreground, Background);
        buffer.DrawBorder(Bounds, Border, BorderColor, Background, Title, Color.Yellow);

        var inner = Bounds.Deflate(2, 1);
        var lines = WrapText(Math.Max(1, inner.Width));
        for (int i = 0; i < lines.Count && i < inner.Height; i++)
            buffer.Write(inner.X, inner.Y + i, lines[i], Foreground, Background);

        string hint = "[ Enter ]";
        buffer.Write(Bounds.X + (Bounds.Width - hint.Length) / 2, Bounds.Bottom - 1,
            hint, Color.Yellow, Background, CellStyle.Bold);
    }
}
