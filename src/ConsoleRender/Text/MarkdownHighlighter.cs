namespace ConsoleRender;

/// <summary>
/// Colors Markdown source while it is edited — syntax highlighting, not a rendered preview.
///
/// Recognized: headings (#..######), **bold**, *italic*, `code`, ~~strikethrough~~,
/// [text](url) links, list markers (-, *, +, 1.), quotes (&gt;), fenced code blocks (```)
/// and horizontal rules (---, ***, ___). Marker characters themselves are dimmed. Code
/// blocks are colored by <see cref="FenceHighlighter"/> according to their language.
///
/// Deliberately not recognized: _italic_/__bold__ underscores, setext headings,
/// backslash escapes, ~~~ fences and indented code blocks.
///
/// Instances hold no state between calls and can be shared.
/// </summary>
public sealed class MarkdownHighlighter : ISyntaxHighlighter
{
    public Color HeadingColor { get; set; } = Color.Cyan;
    public Color CodeColor { get; set; } = Color.Orange;
    public Color LinkTextColor { get; set; } = Color.Blue;
    public Color QuoteColor { get; set; } = Color.Gray;
    public Color BulletColor { get; set; } = Color.Yellow;

    /// <summary>
    /// Picks the highlighter for a fenced code block from its info string (the text after
    /// the opening ```). Null, or a null result, colors the block uniformly in
    /// <see cref="CodeColor"/>. Defaults to <see cref="SyntaxHighlighters.ForFence"/>.
    /// </summary>
    public Func<string, ISyntaxHighlighter?>? FenceHighlighter { get; set; } = SyntaxHighlighters.ForFence;

    public IReadOnlyList<IReadOnlyList<HighlightSpan>> Highlight(IEnumerable<string> lines)
    {
        Guard.Against.Null(lines);

        var list = lines as IReadOnlyList<string> ?? lines.ToList();
        var result = new List<IReadOnlyList<HighlightSpan>>(list.Count);
        var i = 0;
        while (i < list.Count)
        {
            // A fence changes the meaning of everything below it, which is exactly why the
            // interface hands over the whole document: the block body goes to its own
            // highlighter in one piece. An unclosed fence runs to the end.
            if (IsFence(list[i], out var info))
            {
                result.Add(FenceSpans(list[i]));
                var close = i + 1;
                while (close < list.Count && !IsFence(list[close], out _))
                {
                    close++;
                }

                result.AddRange(HighlightFenceBody(info, list, i + 1, close));
                if (close < list.Count)
                {
                    result.Add(FenceSpans(list[close]));
                }

                i = close + 1;
                continue;
            }

            var spans = new List<HighlightSpan>();
            HighlightLine(list[i], spans);
            result.Add(spans);
            i++;
        }

        return result;
    }

    private static bool IsFence(string line, out string info)
    {
        var trimmed = line.TrimStart(' ');
        var isFence = line.Length - trimmed.Length <= 3 && trimmed.StartsWith("```", StringComparison.Ordinal);
        info = isFence ? trimmed[3..] : "";
        return isFence;
    }

    /// <summary>The ``` marker dims, an info string after it takes the code color.</summary>
    private List<HighlightSpan> FenceSpans(string line)
    {
        var trimmed = line.TrimStart(' ');
        var indent = line.Length - trimmed.Length;
        var spans = new List<HighlightSpan> { new(indent, 3, Color.Default, CellStyle.Dim) };
        if (trimmed.Length > 3)
        {
            spans.Add(new(indent + 3, trimmed.Length - 3, CodeColor, CellStyle.None));
        }

        return spans;
    }

    private IEnumerable<IReadOnlyList<HighlightSpan>> HighlightFenceBody(string info,
        IReadOnlyList<string> lines, int start, int end)
    {
        var body = new List<string>(end - start);
        for (var i = start; i < end; i++)
        {
            body.Add(lines[i]);
        }

        if (FenceHighlighter?.Invoke(info) is { } highlighter)
        {
            return highlighter.Highlight(body);
        }

        return body.Select(line => (IReadOnlyList<HighlightSpan>)(line.Length > 0
            ? [new HighlightSpan(0, line.Length, CodeColor, CellStyle.None)]
            : []));
    }

    private void HighlightLine(string line, List<HighlightSpan> spans)
    {
        var trimmed = line.TrimStart(' ');
        var indent = line.Length - trimmed.Length;

        // Rules before lists: "---" is a rule, "- item" a list entry.
        if (IsRule(trimmed))
        {
            spans.Add(new(0, line.Length, Color.Default, CellStyle.Dim));
            return;
        }

        // Heading. The title is deliberately not scanned for inline markers.
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            var hashes = 0;
            while (hashes < trimmed.Length && trimmed[hashes] == '#')
            {
                hashes++;
            }

            if (hashes <= 6 && hashes < trimmed.Length && trimmed[hashes] == ' ')
            {
                spans.Add(new(indent, hashes, Color.Default, CellStyle.Dim));
                var start = indent + hashes + 1;
                if (start < line.Length)
                {
                    spans.Add(new(start, line.Length - start, HeadingColor, CellStyle.Bold));
                }

                return;
            }
        }

        // Quote: the text inherits color and italics, inline markers still work inside.
        if (trimmed.StartsWith(">", StringComparison.Ordinal))
        {
            spans.Add(new(indent, 1, Color.Default, CellStyle.Dim));
            var start = indent + 1;
            if (start < line.Length && line[start] == ' ')
            {
                start++;
            }

            ScanInline(line, start, line.Length, QuoteColor, CellStyle.Italic, spans);
            return;
        }

        // List markers need the trailing space — that keeps "* item" apart from "*italic*".
        var content = indent;
        if (trimmed.Length >= 2 && trimmed[0] is '-' or '*' or '+' && trimmed[1] == ' ')
        {
            spans.Add(new(indent, 1, BulletColor, CellStyle.None));
            content = indent + 2;
        }
        else
        {
            var digits = 0;
            while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits]))
            {
                digits++;
            }

            if (digits > 0 && digits + 1 < trimmed.Length
                && trimmed[digits] == '.' && trimmed[digits + 1] == ' ')
            {
                spans.Add(new(indent, digits + 1, BulletColor, CellStyle.None));
                content = indent + digits + 2;
            }
        }

        ScanInline(line, content, line.Length, Color.Default, CellStyle.None, spans);
    }

    /// <summary>A rule is nothing but three or more of the same marker, spaces allowed.</summary>
    private static bool IsRule(string trimmed)
    {
        if (trimmed.Length == 0)
        {
            return false;
        }

        var marker = trimmed[0];
        if (marker is not ('-' or '*' or '_'))
        {
            return false;
        }

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

    /// <summary>
    /// Scans [start, end) left to right for inline markers. Inherited color/style come from
    /// the enclosing construct (a quote, an outer emphasis); when they carry anything, the
    /// plain-text gaps are emitted as spans too, so the whole segment stays covered without
    /// ever producing an overlap.
    /// </summary>
    private void ScanInline(string line, int start, int end, Color color, CellStyle style, List<HighlightSpan> spans)
    {
        var emitBase = style != CellStyle.None || !color.IsDefault;
        var gapStart = start;
        var pos = start;

        void FlushGap(int upTo)
        {
            if (emitBase && upTo > gapStart)
            {
                spans.Add(new(gapStart, upTo - gapStart, color, style));
            }
        }

        while (pos < end)
        {
            var c = line[pos];

            // Code first: everything between backticks is off-limits for other markers.
            if (c == '`')
            {
                var close = line.IndexOf('`', pos + 1, end - pos - 1);
                if (close > pos)
                {
                    FlushGap(pos);
                    spans.Add(new(pos, 1, Color.Default, CellStyle.Dim));
                    if (close > pos + 1)
                    {
                        spans.Add(new(pos + 1, close - pos - 1, CodeColor, style));
                    }

                    spans.Add(new(close, 1, Color.Default, CellStyle.Dim));
                    pos = gapStart = close + 1;
                    continue;
                }
            }
            else if (c == '~' && Matches(line, pos, end, "~~"))
            {
                if (TryEmphasis(line, pos, end, "~~", color, style | CellStyle.Strikethrough,
                        spans, FlushGap, out var next))
                {
                    pos = gapStart = next;
                    continue;
                }
            }
            else if (c == '*')
            {
                // Longest marker first, so ***x*** ends up bold AND italic.
                if (Matches(line, pos, end, "***")
                    && TryEmphasis(line, pos, end, "***", color,
                        style | CellStyle.Bold | CellStyle.Italic, spans, FlushGap, out var next))
                {
                    pos = gapStart = next;
                    continue;
                }

                if (Matches(line, pos, end, "**")
                    && TryEmphasis(line, pos, end, "**", color, style | CellStyle.Bold,
                        spans, FlushGap, out next))
                {
                    pos = gapStart = next;
                    continue;
                }

                if (TryEmphasis(line, pos, end, "*", color, style | CellStyle.Italic,
                        spans, FlushGap, out next))
                {
                    pos = gapStart = next;
                    continue;
                }
            }
            else if (c == '[' && TryLink(line, pos, end, style, spans, FlushGap, out var next))
            {
                pos = gapStart = next;
                continue;
            }

            pos++;
        }

        FlushGap(end);
    }

    /// <summary>
    /// An emphasis pair: dim markers, content re-scanned with the combined flags — nesting
    /// becomes adjacent spans with OR-ed styles. An unpaired marker stays literal text.
    /// </summary>
    private bool TryEmphasis(string line, int pos, int end, string marker, Color color,
        CellStyle innerStyle, List<HighlightSpan> spans, Action<int> flushGap, out int next)
    {
        var contentStart = pos + marker.Length;
        var close = IndexOf(line, marker, contentStart, end);
        // Empty emphasis ("**" right next to "**") is literal text, matching CommonMark.
        if (close <= contentStart)
        {
            next = pos;
            return false;
        }

        flushGap(pos);
        spans.Add(new(pos, marker.Length, Color.Default, CellStyle.Dim));
        ScanInline(line, contentStart, close, color, innerStyle, spans);
        spans.Add(new(close, marker.Length, Color.Default, CellStyle.Dim));
        next = close + marker.Length;
        return true;
    }

    /// <summary>[text](url): brackets dim, text underlined, url dim.</summary>
    private bool TryLink(string line, int pos, int end, CellStyle style,
        List<HighlightSpan> spans, Action<int> flushGap, out int next)
    {
        next = pos;
        var closeBracket = line.IndexOf(']', pos + 1, end - pos - 1);
        if (closeBracket < 0 || closeBracket + 1 >= end || line[closeBracket + 1] != '(')
        {
            return false;
        }

        var closeParen = line.IndexOf(')', closeBracket + 2, end - closeBracket - 2);
        if (closeParen < 0)
        {
            return false;
        }

        flushGap(pos);
        spans.Add(new(pos, 1, Color.Default, CellStyle.Dim));
        if (closeBracket > pos + 1)
        {
            spans.Add(new(pos + 1, closeBracket - pos - 1, LinkTextColor, style | CellStyle.Underline));
        }

        spans.Add(new(closeBracket, 2, Color.Default, CellStyle.Dim));
        if (closeParen > closeBracket + 2)
        {
            spans.Add(new(closeBracket + 2, closeParen - closeBracket - 2, Color.Default, CellStyle.Dim));
        }

        spans.Add(new(closeParen, 1, Color.Default, CellStyle.Dim));
        next = closeParen + 1;
        return true;
    }

    private static bool Matches(string line, int pos, int end, string token)
    {
        return pos + token.Length <= end
            && string.CompareOrdinal(line, pos, token, 0, token.Length) == 0;
    }

    private static int IndexOf(string line, string token, int from, int end)
    {
        var index = line.IndexOf(token, from, end - from, StringComparison.Ordinal);
        return index < 0 || index + token.Length > end ? -1 : index;
    }
}
