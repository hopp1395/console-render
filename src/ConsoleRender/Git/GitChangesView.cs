namespace ConsoleRender;

/// <summary>
/// An overview of a repository's uncommitted changes: a summary at the top — branch,
/// outgoing (↑) and incoming (↓) commits, one row per changed file with its line counts —
/// and below it one <see cref="DiffView"/> over all files.
///
/// The view only displays; reading is up to the host, typically
/// <c>view.Changes = new GitClient().Read(folder)</c> on demand or on a timer. Setting the
/// same diff again keeps the scroll position, so periodic refreshes do not jump.
///
/// The view is a single Tab stop; when focused it scrolls the diff with the
/// <see cref="DiffView"/> keys (↑/↓, PageUp/PageDown, Home/End, ←/→, N/P).
/// </summary>
public class GitChangesView : Control
{
    private readonly GitChangeSummary summary = new() { Left = 0, Top = 0, Right = 0, Height = 1 };
    private readonly DiffView diff = new() { Left = 0, Right = 0, Bottom = 0 };
    private GitChanges? changes;
    private int maxSummaryRows = 12;

    public GitChangesView()
    {
        Focusable = true;
        diff.Focusable = false;
        AddRange(summary, diff);
        Apply();
    }

    /// <summary>What to show; null until the host has read a repository.</summary>
    public GitChanges? Changes
    {
        get => changes;
        set
        {
            changes = value;
            Apply();
        }
    }

    public DiffLayout Layout
    {
        get => diff.Layout;
        set => diff.Layout = value;
    }

    /// <summary>Whether the diff currently shows side by side (see <see cref="DiffView.IsSideBySide"/>).</summary>
    public bool IsSideBySide => diff.IsSideBySide;

    /// <summary>The most rows the summary may take; further files are summed up in one row.</summary>
    public int MaxSummaryRows
    {
        get => maxSummaryRows;
        set => maxSummaryRows = Guard.Against.NegativeOrZero(value);
    }

    public Color ErrorColor { get; set; } = Color.Red;

    private void Apply()
    {
        var ok = changes is { Error: null };
        summary.Changes = changes;
        summary.Visible = ok;
        diff.Visible = ok;

        var text = ok ? changes!.Diff : "";
        if (diff.Diff != text)
        {
            diff.Diff = text;
        }
    }

    protected override void ArrangeChildren()
    {
        summary.Height = summary.PreferredHeight(maxSummaryRows);
        // One empty row between the summary and the diff.
        diff.Top = summary.Height + 1;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        return diff.Visible && diff.OnKey(key);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (changes is null)
        {
            buffer.Write(Bounds.X, Bounds.Y, "No repository read yet.", Color.DarkGray, default, CellStyle.Italic);
        }
        else if (changes.Error is { } error)
        {
            buffer.Write(Bounds.X, Bounds.Y, error, ErrorColor);
        }
    }
}
