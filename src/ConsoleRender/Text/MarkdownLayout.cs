namespace ConsoleRender;

/// <summary>
/// Turns Markdown into finished screen rows for a given width — the engine behind
/// <see cref="MarkdownView"/>. Block structure is recognized line by line (the same
/// constructs <see cref="MarkdownHighlighter"/> knows), inline markup goes through the
/// shared <see cref="MarkdownInlineScanner"/> with its markers removed, and paragraphs,
/// list items and quotes are word-wrapped with their spans cut to each row.
/// </summary>
internal sealed class MarkdownLayout
{
    private enum Block
    {
        None,
        Paragraph,
        List,
        Other,
    }

    public Color HeadingColor { get; init; } = Color.Cyan;
    public Color CodeColor { get; init; } = Color.Orange;
    public Color LinkTextColor { get; init; } = Color.Blue;
    public Color QuoteColor { get; init; } = Color.Gray;
    public Color BulletColor { get; init; } = Color.Yellow;
    public Color CodeBackground { get; init; } = Color.Rgb(40, 40, 50);
    public Func<string, ISyntaxHighlighter?>? FenceHighlighter { get; init; } = SyntaxHighlighters.ForFence;

    public IReadOnlyList<StyledLine> Build(string markdown, int width)
    {
        Guard.Against.Null(markdown);
        Guard.Against.NegativeOrZero(width);

        var lines = SplitLines(markdown);
        var output = new List<StyledLine>();
        var last = Block.None;
        var i = 0;

        // Blocks are separated by one empty row; consecutive list items stay together.
        void Separate(Block next)
        {
            if (last != Block.None && !(last == Block.List && next == Block.List))
            {
                output.Add(new StyledLine("", [], Color.Default));
            }

            last = next;
        }

        while (i < lines.Count)
        {
            var line = lines[i];
            var trimmed = line.TrimStart(' ');
            var indent = line.Length - trimmed.Length;

            if (trimmed.Length == 0)
            {
                i++;
                continue;
            }

            if (IsFence(line, out var info))
            {
                var close = i + 1;
                while (close < lines.Count && !IsFence(lines[close], out _))
                {
                    close++;
                }

                Separate(Block.Other);
                AddCode(output, info, lines.Skip(i + 1).Take(close - i - 1).ToList());
                i = close + 1;
                continue;
            }

            if (TryHeading(trimmed, out var level, out var title))
            {
                Separate(Block.Other);
                var (color, style) = level switch
                {
                    1 => (HeadingColor, CellStyle.Bold | CellStyle.Underline),
                    2 => (HeadingColor, CellStyle.Bold),
                    _ => (Color.Default, CellStyle.Bold),
                };

                AddWrapped(output, title, color, style, width, "", "", Color.Default);
                i++;
                continue;
            }

            if (IsRule(trimmed))
            {
                Separate(Block.Other);
                output.Add(new StyledLine(new string('─', width),
                    [new HighlightSpan(0, width, Color.Default, CellStyle.Dim)], Color.Default));
                i++;
                continue;
            }

            if (trimmed.StartsWith('>'))
            {
                Separate(Block.Other);
                i = AddQuote(output, lines, i, width);
                continue;
            }

            if (TryListItem(trimmed, out var marker, out var contentOffset))
            {
                Separate(Block.List);
                var text = trimmed[contentOffset..];
                i++;
                while (i < lines.Count && IsContinuation(lines[i]))
                {
                    text += " " + lines[i].Trim();
                    i++;
                }

                // Nesting follows the source indentation, capped so deep lists stay readable.
                var pad = new string(' ', Math.Min(indent, width / 3));
                AddWrapped(output, text, Color.Default, CellStyle.None, width,
                    pad + marker + " ", pad + new string(' ', marker.Length + 1), BulletColor);
                continue;
            }

            Separate(Block.Paragraph);
            var paragraph = trimmed;
            i++;
            while (i < lines.Count && IsContinuation(lines[i]))
            {
                paragraph += " " + lines[i].Trim();
                i++;
            }

            AddWrapped(output, paragraph, Color.Default, CellStyle.None, width, "", "", Color.Default);
        }

        return output;
    }

    /// <summary>A line that continues the paragraph or list item above it.</summary>
    private static bool IsContinuation(string line)
    {
        var trimmed = line.TrimStart(' ');
        return trimmed.Length > 0
            && !IsFence(line, out _)
            && !TryHeading(trimmed, out _, out _)
            && !IsRule(trimmed)
            && !trimmed.StartsWith('>')
            && !TryListItem(trimmed, out _, out _);
    }

    /// <summary>
    /// A run of "&gt;" lines. Its text is gray italic behind a "│" gutter; an empty quote
    /// line separates paragraphs inside the quote. Returns the index after the run.
    /// </summary>
    private int AddQuote(List<StyledLine> output, List<string> lines, int start, int width)
    {
        var i = start;
        var paragraph = "";
        var gutter = new HighlightSpan(0, 1, QuoteColor, CellStyle.None);

        void Flush()
        {
            if (paragraph.Length > 0)
            {
                AddWrapped(output, paragraph, QuoteColor, CellStyle.Italic, width, "│ ", "│ ", QuoteColor);
                paragraph = "";
            }
        }

        while (i < lines.Count && lines[i].TrimStart(' ').StartsWith('>'))
        {
            var content = lines[i].TrimStart(' ')[1..].Trim();
            if (content.Length == 0)
            {
                Flush();
                output.Add(new StyledLine("│", [gutter], Color.Default));
            }
            else
            {
                paragraph = paragraph.Length == 0 ? content : paragraph + " " + content;
            }

            i++;
        }

        Flush();
        return i;
    }

    /// <summary>
    /// A fenced block: one row per source line on the code background, never wrapped
    /// (the view clips it), colored by the fence's highlighter or uniformly in CodeColor.
    /// </summary>
    private void AddCode(List<StyledLine> output, string info, List<string> body)
    {
        var highlighted = FenceHighlighter?.Invoke(info)?.Highlight(body);
        for (var i = 0; i < body.Count; i++)
        {
            // One cell of padding keeps the code off the edge of its background.
            var text = " " + body[i];
            IReadOnlyList<HighlightSpan> spans = highlighted is not null && i < highlighted.Count
                ? highlighted[i].Select(span => span with { Start = span.Start + 1 }).ToList()
                : body[i].Length > 0 ? [new HighlightSpan(1, body[i].Length, CodeColor, CellStyle.None)] : [];
            output.Add(new StyledLine(text, spans, CodeBackground));
        }
    }

    /// <summary>
    /// Scans the inline markup of <paramref name="text"/>, removes its markers and wraps
    /// the result into rows. The first row starts with <paramref name="firstPrefix"/>,
    /// the others with <paramref name="restPrefix"/>; both are drawn in <paramref name="prefixColor"/>.
    /// </summary>
    private void AddWrapped(List<StyledLine> output, string text, Color color, CellStyle style,
        int width, string firstPrefix, string restPrefix, Color prefixColor)
    {
        var (plain, spans) = StripMarkers(text, color, style);

        var pos = 0;
        var first = true;
        do
        {
            var prefix = first ? firstPrefix : restPrefix;
            var available = Math.Max(1, width - prefix.Length);
            while (!first && pos < plain.Length && plain[pos] == ' ')
            {
                pos++;
            }

            var end = plain.Length - pos <= available ? plain.Length : BreakAt(plain, pos, available);

            var row = new List<HighlightSpan>();
            if (prefix.Length > 0 && !prefixColor.IsDefault)
            {
                row.Add(new HighlightSpan(0, prefix.Length, prefixColor, CellStyle.None));
            }

            foreach (var span in spans)
            {
                var from = Math.Max(span.Start, pos);
                var to = Math.Min(span.Start + span.Length, end);
                if (to > from)
                {
                    row.Add(span with { Start = from - pos + prefix.Length, Length = to - from });
                }
            }

            output.Add(new StyledLine(prefix + plain[pos..end], row, Color.Default));
            pos = end;
            first = false;
        }
        while (pos < plain.Length);
    }

    /// <summary>
    /// The end of a row starting at <paramref name="pos"/> that is too long for
    /// <paramref name="available"/> cells: the last space that fits, or — for a word longer
    /// than the row — a hard break, rather than clipping it.
    /// </summary>
    private static int BreakAt(string plain, int pos, int available)
    {
        var space = plain.LastIndexOf(' ', pos + available, available);
        return space > pos ? space : pos + available;
    }

    /// <summary>The text without its markup characters, with the spans moved to match.</summary>
    private (string Plain, List<HighlightSpan> Spans) StripMarkers(string text, Color color, CellStyle style)
    {
        var inline = new List<MarkdownInlineSpan>();
        new MarkdownInlineScanner(CodeColor, LinkTextColor).Scan(text, 0, text.Length, color, style, inline);

        var map = new int[text.Length + 1];
        var plain = new System.Text.StringBuilder(text.Length);
        var markerIndex = 0;
        var markers = inline.Where(span => span.IsMarker).ToList();
        for (var i = 0; i < text.Length; i++)
        {
            while (markerIndex < markers.Count && markers[markerIndex].Start + markers[markerIndex].Length <= i)
            {
                markerIndex++;
            }

            map[i] = plain.Length;
            var inMarker = markerIndex < markers.Count && markers[markerIndex].Start <= i;
            if (!inMarker)
            {
                plain.Append(text[i]);
            }
        }

        map[text.Length] = plain.Length;
        var spans = inline
            .Where(span => !span.IsMarker)
            .Select(span => new HighlightSpan(map[span.Start], span.Length, span.Foreground, span.Style))
            .ToList();
        return (plain.ToString(), spans);
    }

    /// <summary>Lines without breaks; tabs become four spaces, other control characters vanish.</summary>
    private static List<string> SplitLines(string markdown)
    {
        var clean = new System.Text.StringBuilder(markdown.Length);
        foreach (var c in markdown.Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            if (c == '\t')
            {
                clean.Append("    ");
            }
            else if (c >= ' ' || c == '\n')
            {
                clean.Append(c);
            }
        }

        return clean.ToString().Split('\n').ToList();
    }

    private static bool IsFence(string line, out string info)
    {
        var trimmed = line.TrimStart(' ');
        var isFence = line.Length - trimmed.Length <= 3 && trimmed.StartsWith("```", StringComparison.Ordinal);
        info = isFence ? trimmed[3..] : "";
        return isFence;
    }

    private static bool TryHeading(string trimmed, out int level, out string title)
    {
        level = 0;
        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        var ok = level is > 0 and <= 6 && level < trimmed.Length && trimmed[level] == ' ';
        title = ok ? trimmed[(level + 1)..].Trim() : "";
        return ok;
    }

    /// <summary>A rule is nothing but three or more of the same marker, spaces allowed.</summary>
    private static bool IsRule(string trimmed)
    {
        if (trimmed.Length == 0 || trimmed[0] is not ('-' or '*' or '_'))
        {
            return false;
        }

        var marker = trimmed[0];
        var count = 0;
        foreach (var c in trimmed)
        {
            if (c == marker)
            {
                count++;
            }
            else if (c != ' ')
            {
                return false;
            }
        }

        return count >= 3;
    }

    /// <summary>"- ", "* ", "+ " become a bullet; "12. " keeps its number.</summary>
    private static bool TryListItem(string trimmed, out string marker, out int contentOffset)
    {
        marker = "";
        contentOffset = 0;
        if (trimmed.Length >= 2 && trimmed[0] is '-' or '*' or '+' && trimmed[1] == ' ')
        {
            marker = "•";
            contentOffset = 2;
            return true;
        }

        var digits = 0;
        while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits]))
        {
            digits++;
        }

        if (digits > 0 && digits + 1 < trimmed.Length && trimmed[digits] == '.' && trimmed[digits + 1] == ' ')
        {
            marker = trimmed[..(digits + 1)];
            contentOffset = digits + 2;
            return true;
        }

        return false;
    }
}
