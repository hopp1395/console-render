namespace ConsoleRender;

/// <summary>A fixed-width column of a <see cref="Table"/>, with its cells' text alignment.</summary>
public readonly record struct TableColumn(string Header, int Width, TextAlignment Alignment = TextAlignment.Left);

/// <summary>
/// A scrollable table with fixed-width columns. Arrow keys move the row selection (clamped
/// to the first/last row, the same as <see cref="SelectMenu"/> — a table's rows are a list
/// to scroll through, not a cycle), Enter activates the selected row.
/// </summary>
public class Table : Control
{
    /// <summary>How many characters an overflowing cell scrolls per second.</summary>
    private const double ScrollCharsPerSecond = 3.0;

    /// <summary>How long the scroll holds at the end before resetting to the start.</summary>
    private const double ScrollPauseSeconds = 1.0;

    private int scroll;
    private int selectedIndex;

    /// <summary>Time since the current row was selected; drives, and resets, its scroll cycle.</summary>
    private double rowElapsed;

    public List<TableColumn> Columns { get; } = new();
    public List<IReadOnlyList<string>> Rows { get; } = new();

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;
    public Color HeaderColor { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Magenta;
    public Color BorderColor { get; set; } = Color.DarkGray;

    /// <summary>Which row is highlighted.</summary>
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (Rows.Count == 0) { selectedIndex = 0; return; }
            Guard.Against.OutOfRange(value, nameof(value), 0, Rows.Count - 1);
            Select(value);
        }
    }

    /// <summary>Raised when the highlighted row changes.</summary>
    public event Action<int>? SelectionChanged;

    /// <summary>Raised when Enter is pressed on the highlighted row.</summary>
    public event Action<int>? RowActivated;

    public Table()
    {
        Focusable = true;
    }

    public void AddColumn(string header, int width, TextAlignment alignment = TextAlignment.Left)
    {
        Guard.Against.NullOrWhiteSpace(header);
        Guard.Against.NegativeOrZero(width);
        Columns.Add(new TableColumn(header, width, alignment));
    }

    /// <summary>Adds a row. <paramref name="cells"/> must have exactly one entry per column.</summary>
    public void AddRow(params string[] cells)
    {
        Guard.Against.Null(cells);
        if (cells.Length != Columns.Count)
            throw new ArgumentException($"Expected {Columns.Count} cell(s), got {cells.Length}.", nameof(cells));

        Rows.Add(cells);
    }

    protected override Size GetPreferredSize(Size available)
    {
        return new(RowWidth(), Rows.Count + 1);
    }

    /// <summary>Advances the scroll animation of the selected row's overflowing cells.</summary>
    public override void Update(TimeSpan delta)
    {
        Guard.Against.Negative(delta);
        rowElapsed += delta.TotalSeconds;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (Rows.Count == 0) return false;

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                Select(Math.Max(0, selectedIndex - 1));
                return true;
            case ConsoleKey.DownArrow:
                Select(Math.Min(Rows.Count - 1, selectedIndex + 1));
                return true;
            case ConsoleKey.Home:
                Select(0);
                return true;
            case ConsoleKey.End:
                Select(Rows.Count - 1);
                return true;
            case ConsoleKey.Enter:
                RowActivated?.Invoke(selectedIndex);
                return true;
        }
        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1 || Bounds.Width < 1 || Columns.Count == 0) return;

        DrawRow(buffer, Bounds.Y, Columns.Select(c => c.Header).ToArray(),
            HeaderColor, CellStyle.Bold | CellStyle.Underline, scrollOverflow: false);

        int visibleRows = Bounds.Height - 1;
        if (visibleRows <= 0) return;

        // Keep the selection in view.
        if (selectedIndex < scroll) scroll = selectedIndex;
        if (selectedIndex >= scroll + visibleRows) scroll = selectedIndex - visibleRows + 1;

        for (int row = 0; row < visibleRows; row++)
        {
            int i = scroll + row;
            if (i >= Rows.Count) break;

            bool selected = i == selectedIndex;
            var style = selected && Focused ? CellStyle.Reverse | CellStyle.Bold
                : selected ? CellStyle.Bold
                : CellStyle.None;
            var fg = selected ? AccentColor : Foreground;
            DrawRow(buffer, Bounds.Y + 1 + row, Rows[i], fg, style, scrollOverflow: selected);
        }
    }

    private void DrawRow(ConsoleBuffer buffer, int y, IReadOnlyList<string> cells, Color fg, CellStyle style,
        bool scrollOverflow)
    {
        int x = Bounds.X;
        for (int c = 0; c < Columns.Count; c++)
        {
            var column = Columns[c];
            string cell = cells[c];
            // A scrolling cell already fills the column exactly, so alignment does not apply to it.
            string text = scrollOverflow && cell.Length > column.Width
                ? ScrollText(cell, column.Width)
                : Align(cell, column.Width, column.Alignment);
            buffer.Write(x, y, text, fg, Background, style);
            x += column.Width;

            if (c < Columns.Count - 1)
            {
                buffer.Write(x, y, "│", BorderColor, Background);
                x += 1;
            }
        }
    }

    // Scrolls from the start to the point where the last character becomes visible, holds
    // there for ScrollPauseSeconds, then jumps back to the start and scrolls again — timed
    // from "rowElapsed", which Select() resets to zero whenever a different row is selected.
    private string ScrollText(string text, int width)
    {
        int maxOffset = text.Length - width;
        double scrollDuration = maxOffset / ScrollCharsPerSecond;
        double cycleDuration = scrollDuration + ScrollPauseSeconds;

        double t = rowElapsed % cycleDuration;
        int offset = t < scrollDuration ? (int)(t * ScrollCharsPerSecond) : maxOffset;
        return text.Substring(offset, width);
    }

    private void Select(int index)
    {
        if (index == selectedIndex) return;

        selectedIndex = index;
        rowElapsed = 0;
        SelectionChanged?.Invoke(selectedIndex);
    }

    private int RowWidth()
    {
        return Columns.Count == 0 ? 0 : Columns.Sum(c => c.Width) + (Columns.Count - 1);
    }

    private static string Align(string text, int width, TextAlignment alignment)
    {
        string truncated = Truncate(text, width);
        int pad = width - truncated.Length;
        return alignment switch
        {
            TextAlignment.Right => new string(' ', pad) + truncated,
            TextAlignment.Center => new string(' ', pad / 2) + truncated + new string(' ', pad - pad / 2),
            _ => truncated.PadRight(width),
        };
    }

    private static string Truncate(string s, int max)
    {
        return s.Length > max ? s[..max] : s;
    }
}
