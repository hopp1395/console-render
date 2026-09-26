namespace ConsoleRender;

/// <summary>
/// The inline part of Markdown — `code`, **bold**, *italic*, ~~strike~~ and [links](url) —
/// shared by <see cref="MarkdownHighlighter"/> (which dims the markers) and
/// <see cref="MarkdownView"/> (which drops them). Spans come out sorted and overlap-free.
/// </summary>
internal sealed class MarkdownInlineScanner
{
    private readonly Color codeColor;
    private readonly Color linkTextColor;

    public MarkdownInlineScanner(Color codeColor, Color linkTextColor)
    {
        this.codeColor = codeColor;
        this.linkTextColor = linkTextColor;
    }

    /// <summary>
    /// Scans [start, end) left to right for inline markers. Inherited color/style come from
    /// the enclosing construct (a quote, an outer emphasis); when they carry anything, the
    /// plain-text gaps are emitted as spans too, so the whole segment stays covered without
    /// ever producing an overlap.
    /// </summary>
    public void Scan(string line, int start, int end, Color color, CellStyle style, List<MarkdownInlineSpan> spans)
    {
        var emitBase = style != CellStyle.None || !color.IsDefault;
        var gapStart = start;
        var pos = start;

        void FlushGap(int upTo)
        {
            if (emitBase && upTo > gapStart)
            {
                spans.Add(new(gapStart, upTo - gapStart, color, style, false));
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
                    spans.Add(MarkdownInlineSpan.Marker(pos, 1));
                    if (close > pos + 1)
                    {
                        spans.Add(new(pos + 1, close - pos - 1, codeColor, style, false));
                    }

                    spans.Add(MarkdownInlineSpan.Marker(close, 1));
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
        CellStyle innerStyle, List<MarkdownInlineSpan> spans, Action<int> flushGap, out int next)
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
        spans.Add(MarkdownInlineSpan.Marker(pos, marker.Length));
        Scan(line, contentStart, close, color, innerStyle, spans);
        spans.Add(MarkdownInlineSpan.Marker(close, marker.Length));
        next = close + marker.Length;
        return true;
    }

    /// <summary>[text](url): brackets dim, text underlined, url dim.</summary>
    private bool TryLink(string line, int pos, int end, CellStyle style,
        List<MarkdownInlineSpan> spans, Action<int> flushGap, out int next)
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
        spans.Add(MarkdownInlineSpan.Marker(pos, 1));
        if (closeBracket > pos + 1)
        {
            spans.Add(new(pos + 1, closeBracket - pos - 1, linkTextColor, style | CellStyle.Underline, false));
        }

        spans.Add(MarkdownInlineSpan.Marker(closeBracket, 2));
        if (closeParen > closeBracket + 2)
        {
            spans.Add(MarkdownInlineSpan.Marker(closeBracket + 2, closeParen - closeBracket - 2));
        }

        spans.Add(MarkdownInlineSpan.Marker(closeParen, 1));
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
