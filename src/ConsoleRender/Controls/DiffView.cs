namespace ConsoleRender;

/// <summary>
/// A read-only viewer for unified diff text (<c>git diff</c> output): file and hunk
/// headers, line numbers, and changed lines tinted green and red — either in one column or
/// side by side, where a removed block and the added block after it are paired row by row
/// and the shorter side is padded with shaded filler rows.
///
/// When focused: ↑/↓, PageUp/PageDown and Home/End scroll, ←/→ scroll long lines
/// horizontally (both sides together), N and P jump to the next or previous hunk.
/// </summary>
public class DiffView : Control
{
    private enum RowKind
    {
        File,
        Hunk,
        Line,
    }

    /// <summary>
    /// One screen row. Unified rows carry their line in <see cref="Left"/>; side-by-side
    /// rows carry the old side left and the new side right, null meaning a filler cell.
    /// </summary>
    private readonly record struct Row(RowKind Kind, string Text, DiffLine? Left, DiffLine? Right);

    private string diff = "";
    private IReadOnlyList<DiffFile> files = [];
    private int sideBySideMinWidth = 100;

    // Rows depend on the effective layout, which depends on the width — rebuilt only when
    // either the diff or the layout changes, not per frame.
    private List<Row>? rows;
    private bool rowsSideBySide;
    private readonly List<int> hunkRows = new();
    private int numberWidth = 1;
    private int longestLine;

    private int scrollY;
    private int scrollX;

    public DiffView()
    {
        Focusable = true;
    }

    /// <summary>The unified diff to show. Setting it scrolls back to the top.</summary>
    public string Diff
    {
        get => diff;
        set
        {
            diff = Guard.Against.Null(value);
            files = UnifiedDiffParser.Parse(value);
            rows = null;
            scrollY = 0;
            scrollX = 0;
        }
    }

    public DiffLayout Layout { get; set; } = DiffLayout.Auto;

    /// <summary>The width from which <see cref="DiffLayout.Auto"/> switches to side by side.</summary>
    public int SideBySideMinWidth
    {
        get => sideBySideMinWidth;
        set => sideBySideMinWidth = Guard.Against.NegativeOrZero(value);
    }

    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Shown when the diff contains no hunks.</summary>
    public string EmptyText { get; set; } = "No changes.";

    /// <summary>The number of files with at least one hunk.</summary>
    public int FileCount => files.Count;

    /// <summary>Whether the current layout, resolved against the current width, is side by side.</summary>
    public bool IsSideBySide => Layout == DiffLayout.SideBySide
        || (Layout == DiffLayout.Auto && Bounds.Width >= sideBySideMinWidth);

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;
    public Color AddedColor { get; set; } = Color.Green;
    public Color RemovedColor { get; set; } = Color.Red;
    public Color AddedBackground { get; set; } = Color.Rgb(22, 48, 30);
    public Color RemovedBackground { get; set; } = Color.Rgb(58, 26, 26);
    public Color FillerBackground { get; set; } = Color.Rgb(34, 34, 40);
    public Color HunkColor { get; set; } = Color.Cyan;
    public Color HeaderColor { get; set; } = Color.Yellow;
    public Color LineNumberColor { get; set; } = Color.DarkGray;

    protected override Size GetPreferredSize(Size available)
    {
        return new Size(Math.Min(available.Width, 80), Math.Min(available.Height, 16));
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        var page = Math.Max(1, Bounds.Height - 1);
        var plain = (key.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) == 0;

        switch (key.Key)
        {

            case ConsoleKey.UpArrow:
                scrollY--;
                return true;

            case ConsoleKey.DownArrow:
                scrollY++;
                return true;

            case ConsoleKey.PageUp:
                scrollY -= page;
                return true;

            case ConsoleKey.PageDown:
                scrollY += page;
                return true;

            case ConsoleKey.Home:
                scrollY = 0;
                scrollX = 0;
                return true;

            case ConsoleKey.End:
                // Draw clamps this to the last page.
                scrollY = int.MaxValue / 2;
                return true;

            case ConsoleKey.LeftArrow:
                scrollX = Math.Max(0, scrollX - 4);
                return true;

            case ConsoleKey.RightArrow:
                scrollX += 4;
                return true;

            case ConsoleKey.N when plain:
                JumpToHunk(forward: true);
                return true;

            case ConsoleKey.P when plain:
                JumpToHunk(forward: false);
                return true;

        }

        return false;
    }

    private void JumpToHunk(bool forward)
    {
        EnsureRows(IsSideBySide);
        if (forward)
        {
            var next = hunkRows.FirstOrDefault(row => row > scrollY, -1);
            if (next >= 0)
            {
                scrollY = next;
            }
        }
        else
        {
            var previous = hunkRows.LastOrDefault(row => row < scrollY, -1);
            if (previous >= 0)
            {
                scrollY = previous;
            }
        }
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1)
        {
            return;
        }

        var sideBySide = IsSideBySide;
        var built = EnsureRows(sideBySide);
        buffer.FillRect(Bounds, ' ', Foreground, Background);

        if (built.Count == 0)
        {
            buffer.Write(Bounds.X, Bounds.Y, EmptyText, LineNumberColor, Background, CellStyle.Italic);
            return;
        }

        // Scroll clamping lives here and only here, so key handlers, resizes and a new
        // diff all heal on the next frame.
        scrollY = Math.Clamp(scrollY, 0, Math.Max(0, built.Count - Bounds.Height));
        scrollX = Math.Clamp(scrollX, 0, longestLine);

        for (var r = 0; r < Bounds.Height && scrollY + r < built.Count; r++)
        {
            DrawRow(buffer, built[scrollY + r], Bounds.Y + r, sideBySide);
        }
    }

    private void DrawRow(ConsoleBuffer buffer, Row row, int y, bool sideBySide)
    {
        var line = new Rect(Bounds.X, y, Bounds.Width, 1);
        switch (row.Kind)
        {

            case RowKind.File:
                buffer.FillRect(line, '─', LineNumberColor, Background);
                buffer.Write(Bounds.X + 1, y, $" {row.Text} ", HeaderColor, Background, CellStyle.Bold);
                break;

            case RowKind.Hunk:
                buffer.Write(Bounds.X, y, row.Text, HunkColor, Background);
                break;

            case RowKind.Line when sideBySide:
                var half = (Bounds.Width - 1) / 2;
                DrawCell(buffer, line with { Width = half }, row.Left, oldSide: true, newSide: false);
                buffer.Set(Bounds.X + half, y, '│', LineNumberColor, Background);
                DrawCell(buffer, new Rect(Bounds.X + half + 1, y, Bounds.Width - half - 1, 1),
                    row.Right, oldSide: false, newSide: true);
                break;

            default:
                DrawCell(buffer, line, row.Left, oldSide: true, newSide: true);
                break;
        }
    }

    /// <summary>
    /// Draws one diff line into <paramref name="cell"/>: tinted background, the requested
    /// line number columns, the +/- marker, then the text scrolled by scrollX and clipped
    /// to the cell so the left half never paints into the right one.
    /// </summary>
    private void DrawCell(ConsoleBuffer buffer, Rect cell, DiffLine? line, bool oldSide, bool newSide)
    {
        if (cell.Width < 1)
        {
            return;
        }

        if (line is null)
        {
            buffer.FillRect(cell, ' ', Foreground, FillerBackground);
            return;
        }

        var (fg, bg, marker, style) = line.Kind switch
        {
            DiffLineKind.Added => (AddedColor, AddedBackground, '+', CellStyle.None),
            DiffLineKind.Removed => (RemovedColor, RemovedBackground, '-', CellStyle.None),
            DiffLineKind.Note => (LineNumberColor, Background, ' ', CellStyle.Italic),
            _ => (Foreground, Background, ' ', CellStyle.None),
        };

        buffer.FillRect(cell, ' ', fg, bg);
        buffer.PushClip(cell);
        try
        {
            var x = cell.X;
            if (ShowLineNumbers)
            {
                if (oldSide)
                {
                    buffer.Write(x, cell.Y, Number(line.OldNumber), LineNumberColor, bg);
                    x += numberWidth + 1;
                }

                if (newSide)
                {
                    buffer.Write(x, cell.Y, Number(line.NewNumber), LineNumberColor, bg);
                    x += numberWidth + 1;
                }
            }

            buffer.Set(x, cell.Y, marker, fg, bg);
            x += 2;

            var text = new Rect(x, cell.Y, Math.Max(0, cell.Right - x), 1);
            buffer.PushClip(text);
            try
            {
                buffer.Write(x - scrollX, cell.Y, line.Text, fg, bg, style);
            }
            finally
            {
                buffer.PopClip();
            }
        }
        finally
        {
            buffer.PopClip();
        }
    }

    private string Number(int? number)
    {
        return (number?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "").PadLeft(numberWidth);
    }

    private List<Row> EnsureRows(bool sideBySide)
    {
        if (rows is not null && rowsSideBySide == sideBySide)
        {
            return rows;
        }

        rows = new List<Row>();
        rowsSideBySide = sideBySide;
        hunkRows.Clear();
        var maxNumber = 0;
        longestLine = 0;

        foreach (var file in files)
        {
            rows.Add(new Row(RowKind.File, file.Title, null, null));
            foreach (var hunk in file.Hunks)
            {
                hunkRows.Add(rows.Count);
                rows.Add(new Row(RowKind.Hunk, hunk.Header, null, null));
                foreach (var line in hunk.Lines)
                {
                    maxNumber = Math.Max(maxNumber, Math.Max(line.OldNumber ?? 0, line.NewNumber ?? 0));
                    longestLine = Math.Max(longestLine, line.Text.Length);
                }

                if (sideBySide)
                {
                    AddSideBySide(rows, hunk);
                }
                else
                {
                    rows.AddRange(hunk.Lines.Select(line => new Row(RowKind.Line, "", line, null)));
                }
            }
        }

        numberWidth = Math.Max(1, maxNumber.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
        return rows;
    }

    /// <summary>
    /// Context lines appear on both sides. A run of removals followed by a run of additions
    /// is a change: paired row by row, the longer run padded with fillers on the other side.
    /// A note sticks to the side of the line it annotates.
    /// </summary>
    private static void AddSideBySide(List<Row> rows, DiffHunk hunk)
    {
        var removed = new List<DiffLine>();
        var added = new List<DiffLine>();
        var lastKind = DiffLineKind.Context;

        void Flush()
        {
            for (var i = 0; i < Math.Max(removed.Count, added.Count); i++)
            {
                rows.Add(new Row(RowKind.Line, "",
                    i < removed.Count ? removed[i] : null,
                    i < added.Count ? added[i] : null));
            }

            removed.Clear();
            added.Clear();
        }

        foreach (var line in hunk.Lines)
        {
            switch (line.Kind)
            {

                case DiffLineKind.Removed:
                    // A removal after additions starts the next change block.
                    if (added.Count > 0)
                    {
                        Flush();
                    }

                    removed.Add(line);
                    break;

                case DiffLineKind.Added:
                    added.Add(line);
                    break;

                case DiffLineKind.Note when lastKind == DiffLineKind.Removed:
                    removed.Add(line);
                    break;

                case DiffLineKind.Note when lastKind == DiffLineKind.Added:
                    added.Add(line);
                    break;

                default:
                    Flush();
                    rows.Add(new Row(RowKind.Line, "", line, line));
                    break;
            }

            if (line.Kind != DiffLineKind.Note)
            {
                lastKind = line.Kind;
            }
        }

        Flush();
    }
}
