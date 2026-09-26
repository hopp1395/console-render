namespace ConsoleRender;

/// <summary>
/// The head of a <see cref="GitChangesView"/>. The first row shows the checked-out branch,
/// outgoing (↑) and incoming (↓) commits and the totals; below it, one row per file with
/// its line counts and a git-style +/- bar. Files that do not fit are summed up in a last
/// "… more" row.
/// </summary>
internal sealed class GitChangeSummary : Control
{
    private const int BarWidth = 20;

    public GitChanges? Changes { get; set; }

    /// <summary>The rows this summary wants: the header plus one per file, capped at <paramref name="max"/>.</summary>
    public int PreferredHeight(int max)
    {
        return Math.Clamp(1 + (Changes?.Files.Count ?? 0), 1, Math.Max(1, max));
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Changes is not { } changes || Bounds.Width < 1 || Bounds.Height < 1)
        {
            return;
        }

        DrawHeader(buffer, changes);

        var rows = Bounds.Height - 1;
        var files = changes.Files;
        var shown = files.Count <= rows ? files.Count : Math.Max(0, rows - 1);
        var pathWidth = Math.Clamp(files.Count == 0 ? 0 : files.Max(file => file.Path.Length), 8,
            Math.Max(8, Bounds.Width - BarWidth - 16));
        var scale = Math.Max(1, files.Count == 0 ? 1 : files.Max(file => file.Added + file.Removed));

        for (var i = 0; i < shown; i++)
        {
            DrawFile(buffer, files[i], Bounds.Y + 1 + i, pathWidth, scale);
        }

        if (shown < files.Count && rows > 0)
        {
            buffer.Write(Bounds.X + 1, Bounds.Y + Bounds.Height - 1,
                $"… {files.Count - shown} more files", Color.DarkGray, default, CellStyle.Italic);
        }
    }

    /// <summary>"main  ↑2 ↓1  3 files changed  +12 −4".</summary>
    private void DrawHeader(ConsoleBuffer buffer, GitChanges changes)
    {
        var x = Bounds.X;
        var y = Bounds.Y;
        x = Write(buffer, x, y, changes.Branch, Color.Cyan, CellStyle.Bold) + 2;

        if (changes.Ahead is { } ahead && changes.Behind is { } behind)
        {
            x = Write(buffer, x, y, $"↑{ahead}", ahead > 0 ? Color.Yellow : Color.DarkGray, CellStyle.None) + 1;
            x = Write(buffer, x, y, $"↓{behind}", behind > 0 ? Color.Magenta : Color.DarkGray, CellStyle.None) + 2;
        }
        else
        {
            x = Write(buffer, x, y, "no upstream", Color.DarkGray, CellStyle.Italic) + 2;
        }

        var count = changes.Files.Count;
        if (count == 0)
        {
            Write(buffer, x, y, "working tree clean", Color.DarkGray, CellStyle.Italic);
            return;
        }

        x = Write(buffer, x, y, count == 1 ? "1 file changed" : $"{count} files changed", Color.Default, CellStyle.None) + 2;
        x = Write(buffer, x, y, $"+{changes.Added}", Color.Green, CellStyle.None) + 1;
        Write(buffer, x, y, $"−{changes.Removed}", Color.Red, CellStyle.None);
    }

    private void DrawFile(ConsoleBuffer buffer, GitFileChange file, int y, int pathWidth, int scale)
    {
        var path = file.Path.Length > pathWidth ? "…" + file.Path[^(pathWidth - 1)..] : file.Path;
        var x = Write(buffer, Bounds.X + 1, y, path.PadRight(pathWidth), Color.Default, CellStyle.None) + 1;

        if (file.IsBinary)
        {
            Write(buffer, x, y, file.IsNew ? "new, binary or large" : "binary", Color.DarkGray, CellStyle.Italic);
            return;
        }

        x = Write(buffer, x, y, $"+{file.Added}".PadLeft(6), Color.Green, CellStyle.None);
        x = Write(buffer, x, y, $"−{file.Removed}".PadLeft(6), Color.Red, CellStyle.None) + 1;

        // Like git --stat: the bar is scaled to the largest change.
        var plus = (int)Math.Ceiling((double)file.Added * BarWidth / scale);
        var minus = (int)Math.Ceiling((double)file.Removed * BarWidth / scale);
        x = Write(buffer, x, y, new string('+', plus), Color.Green, CellStyle.None);
        x = Write(buffer, x, y, new string('-', minus), Color.Red, CellStyle.None);

        if (file.IsNew)
        {
            Write(buffer, x + 1, y, "new", Color.DarkGray, CellStyle.Italic);
        }
    }

    /// <summary>Writes and returns the column after the text.</summary>
    private static int Write(ConsoleBuffer buffer, int x, int y, string text, Color color, CellStyle style)
    {
        buffer.Write(x, y, text, color, default, style);
        return x + text.Length;
    }
}
