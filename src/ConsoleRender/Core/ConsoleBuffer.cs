namespace ConsoleRender;

/// <summary>
/// A 2D grid of cells that controls draw into. All writes are clipped to the buffer bounds,
/// so drawing partially off-screen content is safe.
/// </summary>
public sealed class ConsoleBuffer
{
    private Cell[] cells;
    private readonly Stack<Rect> clips = new();
    private Rect clip;

    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>The region writes are currently restricted to.</summary>
    public Rect ClipRect => clip;

    public ConsoleBuffer(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        Width = width;
        Height = height;
        cells = new Cell[Width * Height];
        ResetClip();
        Clear();
    }

    public Cell this[int x, int y]
    {
        get => cells[y * Width + x];
        set
        {
            if (clip.Contains(x, y))
                cells[y * Width + x] = value;
        }
    }

    /// <summary>
    /// Restricts subsequent writes to the intersection of <paramref name="rect"/> and the
    /// current clip region. Every push must be matched by a <see cref="PopClip"/>.
    /// </summary>
    public void PushClip(Rect rect)
    {
        clips.Push(clip);
        clip = clip.Intersect(rect);
    }

    /// <summary>Restores the clip region that was active before the matching <see cref="PushClip"/>.</summary>
    public void PopClip()
    {
        if (clips.Count == 0)
            throw new InvalidOperationException("PopClip called without a matching PushClip.");
        clip = clips.Pop();
    }

    /// <summary>Drops all clip regions and allows writing to the whole buffer again.</summary>
    public void ResetClip()
    {
        clips.Clear();
        clip = new Rect(0, 0, Width, Height);
    }

    public void Resize(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        Width = width;
        Height = height;
        cells = new Cell[Width * Height];
        ResetClip();
        Clear();
    }

    public void Clear() => Clear(Cell.Empty);

    public void Clear(Cell fill) => Array.Fill(cells, fill);

    public void Set(int x, int y, char ch, Color fg = default, Color bg = default, CellStyle style = CellStyle.None)
        => this[x, y] = new Cell(ch, fg, bg, style);

    /// <summary>Writes a string starting at (x, y). Text running past the right edge is clipped.</summary>
    public void Write(int x, int y, string text, Color fg = default, Color bg = default, CellStyle style = CellStyle.None)
    {
        Guard.Against.Null(text);

        if (y < clip.Y || y >= clip.Bottom) return;
        for (int i = 0; i < text.Length; i++)
        {
            int cx = x + i;
            if (cx >= clip.Right) break;
            if (cx < clip.X) continue;
            char ch = text[i];
            if (ch == '\n' || ch == '\r') break;
            cells[y * Width + cx] = new Cell(ch, fg, bg, style);
        }
    }

    public void FillRect(Rect rect, char ch, Color fg = default, Color bg = default, CellStyle style = CellStyle.None)
    {
        var r = rect.Intersect(clip);
        var cell = new Cell(ch, fg, bg, style);
        for (int y = r.Y; y < r.Bottom; y++)
            for (int x = r.X; x < r.Right; x++)
                cells[y * Width + x] = cell;
    }

    /// <summary>Sets the background color of a rectangle without touching characters.</summary>
    public void TintBackground(Rect rect, Color bg)
    {
        var r = rect.Intersect(clip);
        for (int y = r.Y; y < r.Bottom; y++)
            for (int x = r.X; x < r.Right; x++)
            {
                var c = cells[y * Width + x];
                cells[y * Width + x] = c with { Background = bg };
            }
    }

    /// <summary>Draws a box border along the edge of <paramref name="rect"/>, optionally with a title in the top edge.</summary>
    public void DrawBorder(Rect rect, BorderStyle border, Color fg = default, Color bg = default,
        string? title = null, Color titleColor = default)
    {
        Guard.Against.Null(border);

        if (rect.Width < 2 || rect.Height < 2) return;
        int x2 = rect.Right - 1, y2 = rect.Bottom - 1;

        Set(rect.X, rect.Y, border.TopLeft, fg, bg);
        Set(x2, rect.Y, border.TopRight, fg, bg);
        Set(rect.X, y2, border.BottomLeft, fg, bg);
        Set(x2, y2, border.BottomRight, fg, bg);
        for (int x = rect.X + 1; x < x2; x++)
        {
            Set(x, rect.Y, border.Horizontal, fg, bg);
            Set(x, y2, border.Horizontal, fg, bg);
        }
        for (int y = rect.Y + 1; y < y2; y++)
        {
            Set(rect.X, y, border.Vertical, fg, bg);
            Set(x2, y, border.Vertical, fg, bg);
        }

        if (!string.IsNullOrEmpty(title))
        {
            int maxLen = rect.Width - 4;
            if (maxLen > 0)
            {
                string t = title.Length > maxLen ? title[..maxLen] : title;
                Write(rect.X + 2, rect.Y, $" {t} ",
                    titleColor.IsDefault ? fg : titleColor, bg, CellStyle.Bold);
            }
        }
    }

    /// <summary>
    /// Returns the buffer contents as plain text, one line per row and colors dropped.
    /// Intended for snapshot tests and debugging, not for output.
    /// </summary>
    public string ToText()
    {
        var sb = new System.Text.StringBuilder(Width * Height + Height);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char ch = this[x, y].Char;
                sb.Append(ch == '\0' ? ' ' : ch);
            }
            if (y < Height - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    internal void CopyTo(ConsoleBuffer target)
    {
        Guard.Against.Null(target);

        Array.Copy(cells, target.cells, cells.Length);
    }
}
