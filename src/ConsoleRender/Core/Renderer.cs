using System.Text;

namespace ConsoleRender;

/// <summary>
/// Double-buffered renderer. Controls draw into the back buffer; <see cref="Present"/> diffs
/// against the front buffer and emits only the ANSI sequences needed to update changed cells.
/// </summary>
public sealed class Renderer
{
    private ConsoleBuffer _back;
    private ConsoleBuffer _front;
    private readonly StringBuilder _sb = new(16 * 1024);
    private bool _fullRedraw = true;

    public Renderer(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        _back = new ConsoleBuffer(width, height);
        _front = new ConsoleBuffer(width, height);
    }

    /// <summary>The back buffer to draw the next frame into.</summary>
    public ConsoleBuffer Buffer => _back;

    public int Width => _back.Width;
    public int Height => _back.Height;

    public void Resize(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        if (width == Width && height == Height) return;
        _back.Resize(width, height);
        _front.Resize(width, height);
        _fullRedraw = true;
    }

    /// <summary>Forces the next <see cref="Present"/> to repaint every cell.</summary>
    public void Invalidate() => _fullRedraw = true;

    /// <summary>Flushes the differences between back and front buffer to the terminal.</summary>
    public void Present(TextWriter output)
    {
        Guard.Against.Null(output);

        _sb.Clear();

        int curX = int.MinValue, curY = int.MinValue;
        Color penFg = default, penBg = default;
        CellStyle penStyle = CellStyle.None;
        bool penValid = false;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var cell = _back[x, y];
                if (!_fullRedraw && cell == _front[x, y]) continue;

                if (curY != y || curX != x)
                {
                    _sb.Append("\x1b[").Append(y + 1).Append(';').Append(x + 1).Append('H');
                    curX = x;
                    curY = y;
                }

                if (!penValid || cell.Foreground != penFg || cell.Background != penBg || cell.Style != penStyle)
                {
                    AppendSgr(_sb, cell);
                    penFg = cell.Foreground;
                    penBg = cell.Background;
                    penStyle = cell.Style;
                    penValid = true;
                }

                _sb.Append(cell.Char == '\0' ? ' ' : cell.Char);
                curX++;
            }
        }

        if (_sb.Length > 0)
        {
            _sb.Append("\x1b[0m");
            output.Write(_sb);
            output.Flush();
        }

        _back.CopyTo(_front);
        _fullRedraw = false;
    }

    private static void AppendSgr(StringBuilder sb, in Cell cell)
    {
        sb.Append("\x1b[0");
        var s = cell.Style;
        if ((s & CellStyle.Bold) != 0) sb.Append(";1");
        if ((s & CellStyle.Dim) != 0) sb.Append(";2");
        if ((s & CellStyle.Italic) != 0) sb.Append(";3");
        if ((s & CellStyle.Underline) != 0) sb.Append(";4");
        if ((s & CellStyle.Blink) != 0) sb.Append(";5");
        if ((s & CellStyle.Reverse) != 0) sb.Append(";7");
        if ((s & CellStyle.Strikethrough) != 0) sb.Append(";9");

        var fg = cell.Foreground;
        if (fg.IsDefault) sb.Append(";39");
        else sb.Append(";38;2;").Append(fg.R).Append(';').Append(fg.G).Append(';').Append(fg.B);

        var bg = cell.Background;
        if (bg.IsDefault) sb.Append(";49");
        else sb.Append(";48;2;").Append(bg.R).Append(';').Append(bg.G).Append(';').Append(bg.B);

        sb.Append('m');
    }
}
