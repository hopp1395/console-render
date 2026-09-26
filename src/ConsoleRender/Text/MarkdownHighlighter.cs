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

    /// <summary>Inline markup via the shared scanner; its markers become dimmed spans here.</summary>
    private void ScanInline(string line, int start, int end, Color color, CellStyle style, List<HighlightSpan> spans)
    {
        var inline = new List<MarkdownInlineSpan>();
        new MarkdownInlineScanner(CodeColor, LinkTextColor).Scan(line, start, end, color, style, inline);
        foreach (var span in inline)
        {
            spans.Add(span.IsMarker
                ? new HighlightSpan(span.Start, span.Length, Color.Default, CellStyle.Dim)
                : new HighlightSpan(span.Start, span.Length, span.Foreground, span.Style));
        }
    }
}
