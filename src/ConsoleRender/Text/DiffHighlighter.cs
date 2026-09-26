namespace ConsoleRender;

/// <summary>
/// Colors unified diff text the way <c>git diff</c> does: added lines green, removed lines
/// red, hunk headers cyan, file headers bold and the other header lines dimmed.
///
/// Classification needs the whole document: a line "--- x" is a file header before a hunk
/// but a removed line "-- x" inside one. The highlighter therefore counts the remaining
/// lines of each hunk from its "@@ -a,b +c,d @@" header, as <c>git apply</c> does.
///
/// Instances hold no state between calls and can be shared.
/// </summary>
public sealed class DiffHighlighter : ISyntaxHighlighter
{
    public Color AddedColor { get; set; } = Color.Green;
    public Color RemovedColor { get; set; } = Color.Red;
    public Color HunkColor { get; set; } = Color.Cyan;
    public Color HeaderColor { get; set; } = Color.Default;

    public IReadOnlyList<IReadOnlyList<HighlightSpan>> Highlight(IEnumerable<string> lines)
    {
        Guard.Against.Null(lines);

        var result = new List<IReadOnlyList<HighlightSpan>>();
        var oldLeft = 0;
        var newLeft = 0;
        var inFileHeader = false;

        foreach (var line in lines)
        {
            var spans = new List<HighlightSpan>();
            result.Add(spans);
            if (line.Length == 0)
            {
                continue;
            }

            if (oldLeft > 0 || newLeft > 0)
            {
                if (line[0] == ' ' && oldLeft > 0 && newLeft > 0)
                {
                    oldLeft--;
                    newLeft--;
                    continue;
                }

                if (line[0] == '-' && oldLeft > 0)
                {
                    spans.Add(new(0, line.Length, RemovedColor, CellStyle.None));
                    oldLeft--;
                    continue;
                }

                if (line[0] == '+' && newLeft > 0)
                {
                    spans.Add(new(0, line.Length, AddedColor, CellStyle.None));
                    newLeft--;
                    continue;
                }

                if (line[0] != '\\')
                {
                    oldLeft = newLeft = 0;
                }
            }

            if (line[0] == '\\')
            {
                spans.Add(new(0, line.Length, Color.Default, CellStyle.Dim));
                continue;
            }

            if (UnifiedDiffParser.TryParseHunkHeader(line, out var header))
            {
                spans.Add(new(0, header.End, HunkColor, CellStyle.None));
                oldLeft = header.OldCount;
                newLeft = header.NewCount;
                inFileHeader = false;
                continue;
            }

            if (line.StartsWith("diff ", StringComparison.Ordinal)
                || line.StartsWith("--- ", StringComparison.Ordinal)
                || line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                spans.Add(new(0, line.Length, HeaderColor, CellStyle.Bold));
                inFileHeader = true;
                continue;
            }

            // "index …", "new file mode …", "rename from …" and friends.
            if (inFileHeader)
            {
                spans.Add(new(0, line.Length, Color.Default, CellStyle.Dim));
            }
        }

        return result;
    }
}
