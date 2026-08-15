namespace ConsoleRender;

/// <summary>
/// A bordered message box shown as a modal dialog via <see cref="ConsoleApp.ShowDialog"/>
/// or <see cref="ConsoleApp.ShowInfo"/>. It states something and offers a single way out;
/// use <see cref="ConfirmDialog"/> when the user has to choose.
/// Enter, Escape or Space dismisses it.
/// </summary>
public class InfoBox : ModalControl
{
    private string title = "";
    private string text = "";
    private BorderStyle border = BorderStyle.Double;
    private int maxTextWidth = 48;

    public string Title
    {
        get => title;
        set => title = Guard.Against.Null(value);
    }

    public string Text
    {
        get => text;
        set => text = Guard.Against.Null(value);
    }

    public BorderStyle Border
    {
        get => border;
        set => border = Guard.Against.Null(value);
    }

    public Color BorderColor { get; set; } = Color.Cyan;
    public Color Foreground { get; set; } = Color.White;
    public Color Background { get; set; } = Color.DarkBlue;

    /// <summary>Maximum text width before wrapping.</summary>
    public int MaxTextWidth
    {
        get => maxTextWidth;
        set => maxTextWidth = Guard.Against.NegativeOrZero(value);
    }

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
        return TextWrap.Wrap(Text, width);
    }

    protected override Size GetPreferredSize(Size available)
    {
        var textWidth = Math.Min(MaxTextWidth, Math.Max(10, available.Width - 8));
        var lines = WrapText(textWidth);
        var contentWidth = Math.Max(Title.Length + 2, Math.Max(lines.Count == 0 ? 0 : lines.Max(l => l.Length), 12));
        // Top border, the text, one blank row, and the bottom border that carries the hint.
        return new Size(contentWidth + 4, lines.Count + 3);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        buffer.FillRect(Bounds, ' ', Foreground, Background);
        buffer.DrawBorder(Bounds, Border, BorderColor, Background, Title, Color.Yellow);

        var inner = Bounds.Deflate(2, 1);
        if (inner.Width <= 0)
        {
            return;
        }

        var lines = WrapText(inner.Width);
        for (var i = 0; i < lines.Count && i < inner.Height; i++)
        {
            buffer.Write(inner.X, inner.Y + i, lines[i], Foreground, Background);
        }

        var hint = "[ Enter ]";
        buffer.Write(Bounds.X + (Bounds.Width - hint.Length) / 2, Bounds.Bottom - 1,
            hint, Color.Yellow, Background, CellStyle.Bold);
    }
}
