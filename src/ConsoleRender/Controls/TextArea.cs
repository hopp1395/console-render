namespace ConsoleRender;

/// <summary>
/// A multi-line text input field: a plain editor with a cursor, vertical and horizontal
/// scrolling (no soft wrap — one document line is one screen row), clipboard support and
/// optional syntax highlighting via <see cref="Highlighter"/>.
///
/// Enter inserts a line break; there is deliberately no Submitted event. A host that wants
/// a "send" action registers a key binding (Ctrl+Enter, say) with <see cref="ConsoleApp.KeyBindings"/>.
/// Tab and Escape are not consumed, so focus cycling and modal dismissal keep working.
/// </summary>
public class TextArea : Control
{
    private readonly List<string> lines = new() { "" };

    private int cursorLine;
    private int cursorCol;
    private int desiredCol; // column ↑/↓ steer back to after passing a shorter line
    private int scrollY;
    private int scrollX;

    // The highlighter runs once per edit, not once per frame: every mutation bumps
    // version, and Draw recomputes only when the cache is behind.
    private int version;
    private int highlightedVersion = -1;
    private IReadOnlyList<IReadOnlyList<HighlightSpan>>? highlightCache;
    private ISyntaxHighlighter? highlighter;

    private string placeholder = "";
    private BorderStyle border = BorderStyle.Single;

    public TextArea() => Focusable = true;

    /// <summary>The whole document, lines separated by '\n'.</summary>
    public string Text
    {
        get => string.Join('\n', lines);
        set
        {
            Guard.Against.Null(value);

            lines.Clear();
            foreach (string line in Normalize(value).Split('\n'))
                lines.Add(line);
            cursorLine = lines.Count - 1;
            cursorCol = lines[^1].Length;
            desiredCol = cursorCol;
            Bump();
        }
    }

    /// <summary>The document as individual lines. Entries never contain line breaks.</summary>
    public IReadOnlyList<string> Lines => lines;

    /// <summary>Line the cursor is on (0-based).</summary>
    public int CursorLine => cursorLine;

    /// <summary>Column the cursor is at (0-based; may sit one past the line's end).</summary>
    public int CursorColumn => cursorCol;

    /// <summary>
    /// Whether and how the field frames itself, exactly like <see cref="TextBox.BorderMode"/>.
    /// If the control is too small for the chosen border, the text wins and the border is left out.
    /// </summary>
    public BorderMode BorderMode { get; set; } = BorderMode.None;

    /// <summary>Characters used for the border. Only takes effect with a <see cref="BorderMode"/>.</summary>
    public BorderStyle Border
    {
        get => border;
        set => border = Guard.Against.Null(value);
    }

    public Color BorderColor { get; set; } = Color.Default;

    /// <summary>Hint text shown while the document is empty and unfocused.</summary>
    public string Placeholder
    {
        get => placeholder;
        set => placeholder = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Rgb(38, 38, 46);
    public Color PlaceholderColor { get; set; } = Color.DarkGray;

    /// <summary>
    /// Colors the content while typing (e.g. a <see cref="MarkdownHighlighter"/>);
    /// null renders plain text. The result is cached and recomputed only on edits.
    /// </summary>
    public ISyntaxHighlighter? Highlighter
    {
        get => highlighter;
        set
        {
            highlighter = value;
            highlightedVersion = -1;
            highlightCache = null;
        }
    }

    /// <summary>Raised once per editing operation with the new document text.</summary>
    public event Action<string>? TextChanged;

    protected override Size GetPreferredSize(Size available) => BorderMode switch
    {
        BorderMode.Full => new Size(32, 10),
        BorderMode.TopAndBottom => new Size(30, 10),
        _ => new Size(30, 8),
    };

    /// <summary>True when the current bounds leave room for the chosen border.</summary>
    private bool BorderFits => BorderMode switch
    {
        BorderMode.Full => Bounds.Height >= 3 && Bounds.Width >= 3,
        BorderMode.TopAndBottom => Bounds.Height >= 3,
        _ => false,
    };

    /// <summary>The rows the text is drawn on, inside the border if there is one.</summary>
    private Rect TextRect => !BorderFits
        ? Bounds
        : BorderMode == BorderMode.Full
            ? Bounds.Deflate(1, 1)
            : new Rect(Bounds.X, Bounds.Y + 1, Bounds.Width, Bounds.Height - 2);

    public override bool OnKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
        string line = lines[cursorLine];
        int rows = Math.Max(1, TextRect.Height);

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                lines[cursorLine] = line[..cursorCol];
                lines.Insert(cursorLine + 1, line[cursorCol..]);
                cursorLine++;
                cursorCol = 0;
                desiredCol = 0;
                Bump();
                return true;

            case ConsoleKey.Backspace:
                if (cursorCol > 0)
                {
                    lines[cursorLine] = line.Remove(cursorCol - 1, 1);
                    cursorCol--;
                    desiredCol = cursorCol;
                    Bump();
                }
                else if (cursorLine > 0)
                {
                    cursorCol = lines[cursorLine - 1].Length;
                    desiredCol = cursorCol;
                    lines[cursorLine - 1] += line;
                    lines.RemoveAt(cursorLine);
                    cursorLine--;
                    Bump();
                }
                return true;

            case ConsoleKey.Delete:
                if (cursorCol < line.Length)
                {
                    lines[cursorLine] = line.Remove(cursorCol, 1);
                    Bump();
                }
                else if (cursorLine < lines.Count - 1)
                {
                    lines[cursorLine] += lines[cursorLine + 1];
                    lines.RemoveAt(cursorLine + 1);
                    Bump();
                }
                return true;

            case ConsoleKey.LeftArrow:
                if (cursorCol > 0)
                    cursorCol--;
                else if (cursorLine > 0)
                {
                    cursorLine--;
                    cursorCol = lines[cursorLine].Length;
                }
                desiredCol = cursorCol;
                return true;

            case ConsoleKey.RightArrow:
                if (cursorCol < line.Length)
                    cursorCol++;
                else if (cursorLine < lines.Count - 1)
                {
                    cursorLine++;
                    cursorCol = 0;
                }
                desiredCol = cursorCol;
                return true;

            case ConsoleKey.UpArrow:
                MoveToLine(cursorLine - 1);
                return true;

            case ConsoleKey.DownArrow:
                MoveToLine(cursorLine + 1);
                return true;

            case ConsoleKey.PageUp:
                MoveToLine(cursorLine - rows);
                return true;

            case ConsoleKey.PageDown:
                MoveToLine(cursorLine + rows);
                return true;

            case ConsoleKey.Home when ctrl:
                cursorLine = 0;
                cursorCol = 0;
                desiredCol = 0;
                return true;

            case ConsoleKey.End when ctrl:
                cursorLine = lines.Count - 1;
                cursorCol = lines[^1].Length;
                desiredCol = cursorCol;
                return true;

            case ConsoleKey.Home:
                cursorCol = 0;
                desiredCol = 0;
                return true;

            case ConsoleKey.End:
                cursorCol = line.Length;
                desiredCol = cursorCol;
                return true;

            case ConsoleKey.C when ctrl:
                // NewLine instead of '\n', so applications outside see the line breaks.
                Clipboard.TrySetText(string.Join(Environment.NewLine, lines));
                return true;

            case ConsoleKey.V when ctrl:
                if (Clipboard.TryGetText(out string pasted))
                    Insert(pasted);
                return true;
        }

        if (!ctrl && key.KeyChar >= ' ' && key.KeyChar != '\x7f')
        {
            Insert(key.KeyChar.ToString());
            return true;
        }

        return false;
    }

    /// <summary>Inserts text at the cursor; line breaks in it split the document.</summary>
    public void Insert(string text)
    {
        Guard.Against.Null(text);

        string[] parts = Normalize(text).Split('\n');
        string line = lines[cursorLine];
        string tail = line[cursorCol..];

        lines[cursorLine] = line[..cursorCol] + parts[0];
        for (int i = 1; i < parts.Length; i++)
            lines.Insert(cursorLine + i, parts[i]);

        cursorLine += parts.Length - 1;
        cursorCol = lines[cursorLine].Length;
        lines[cursorLine] += tail;
        desiredCol = cursorCol;
        Bump();
    }

    private void MoveToLine(int target)
    {
        cursorLine = Math.Clamp(target, 0, lines.Count - 1);
        // Steer back to the desired column, clamped by the (possibly shorter) new line.
        cursorCol = Math.Min(desiredCol, lines[cursorLine].Length);
    }

    /// <summary>One text change per editing operation: bump the version, tell the world.</summary>
    private void Bump()
    {
        version++;
        TextChanged?.Invoke(Text);
    }

    /// <summary>
    /// Line entries must never carry line breaks or tabs — <see cref="ConsoleBuffer.Write"/>
    /// stops at control characters. All three entry points (Text setter, Insert, paste)
    /// funnel through here.
    /// </summary>
    private static string Normalize(string text)
    {
        Guard.Against.Null(text);

        var result = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n') continue;
                result.Append('\n');
            }
            else if (c == '\t')
                result.Append("    ");
            else if (c >= ' ' || c == '\n')
                result.Append(c);
        }
        return result.ToString();
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1) return;

        DrawBorder(buffer);

        var area = TextRect;
        if (area.Width < 1 || area.Height < 1) return;

        buffer.PushClip(area);
        try
        {
            DrawContent(buffer, area);
        }
        finally
        {
            buffer.PopClip();
        }
    }

    private void DrawContent(ConsoleBuffer buffer, Rect area)
    {
        buffer.FillRect(area, ' ', Foreground, Background);

        // Scroll clamping lives here and only here, so a resize or an externally replaced
        // Text heals on the next frame without scattered fixups.
        int rows = area.Height;
        if (cursorLine < scrollY) scrollY = cursorLine;
        if (cursorLine >= scrollY + rows) scrollY = cursorLine - rows + 1;
        scrollY = Math.Clamp(scrollY, 0, Math.Max(0, lines.Count - rows));

        // One column of slack keeps the caret visible when it sits past the line's end.
        int visible = Math.Max(1, area.Width - 1);
        if (cursorCol < scrollX) scrollX = cursorCol;
        if (cursorCol > scrollX + visible) scrollX = cursorCol - visible;
        scrollX = Math.Max(0, scrollX);

        bool empty = lines.Count == 1 && lines[0].Length == 0;
        if (empty && !Focused && Placeholder.Length > 0)
        {
            buffer.Write(area.X, area.Y, Placeholder, PlaceholderColor, Background, CellStyle.Italic);
            return;
        }

        var spans = HighlightedSpans();

        for (int r = 0; r < rows && scrollY + r < lines.Count; r++)
        {
            int index = scrollY + r;
            string line = lines[index];
            int y = area.Y + r;

            buffer.Write(area.X - scrollX, y, line, Foreground, Background);

            if (spans is null || index >= spans.Count) continue;
            foreach (var span in spans[index])
            {
                if (span.Start + span.Length <= scrollX || span.Start >= scrollX + area.Width)
                    continue;
                buffer.Write(area.X + span.Start - scrollX, y,
                    line.Substring(span.Start, Math.Min(span.Length, line.Length - span.Start)),
                    span.Foreground.IsDefault ? Foreground : span.Foreground,
                    Background, span.Style);
            }
        }

        if (Focused)
        {
            int cx = area.X + cursorCol - scrollX;
            int cy = area.Y + cursorLine - scrollY;
            // Reverse the cell in place, so a highlight color stays visible under the caret.
            // The clip check also guards the unclipped indexer read.
            if (buffer.ClipRect.Contains(cx, cy))
            {
                var cell = buffer[cx, cy];
                buffer[cx, cy] = cell with { Style = cell.Style | CellStyle.Reverse };
            }
        }
    }

    private IReadOnlyList<IReadOnlyList<HighlightSpan>>? HighlightedSpans()
    {
        if (highlighter is null) return null;
        if (highlightedVersion != version)
        {
            highlightCache = highlighter.Highlight(lines);
            highlightedVersion = version;
        }
        return highlightCache;
    }

    private void DrawBorder(ConsoleBuffer buffer)
    {
        if (!BorderFits) return;

        if (BorderMode == BorderMode.Full)
        {
            buffer.DrawBorder(Bounds, Border, BorderColor);
            return;
        }

        for (int x = Bounds.X; x < Bounds.Right; x++)
        {
            buffer.Set(x, Bounds.Y, Border.Horizontal, BorderColor);
            buffer.Set(x, Bounds.Bottom - 1, Border.Horizontal, BorderColor);
        }
    }
}
