namespace ConsoleRender;

/// <summary>
/// Colors source code by the table in a <see cref="CodeLanguage"/>: keywords, string
/// literals, numbers, comments and — where the language enables them — object keys and
/// variable references. Text between tokens stays uncolored (<see cref="Color.Default"/>).
///
/// The only state carried from line to line is "inside a block comment". String literals
/// end at the line end, so multi-line verbatim or raw strings are not recognized, and a
/// backslash always escapes the next character (shell single quotes included).
///
/// Instances hold no state between calls and can be shared.
/// </summary>
public sealed class CodeHighlighter : ISyntaxHighlighter
{
    public CodeHighlighter(CodeLanguage language)
    {
        Language = Guard.Against.Null(language);
    }

    public CodeLanguage Language { get; }

    public Color KeywordColor { get; set; } = Color.Magenta;
    public Color StringColor { get; set; } = Color.Green;
    public Color NumberColor { get; set; } = Color.Cyan;
    public Color CommentColor { get; set; } = Color.Gray;
    public Color KeyColor { get; set; } = Color.Blue;
    public Color VariableColor { get; set; } = Color.Yellow;

    public IReadOnlyList<IReadOnlyList<HighlightSpan>> Highlight(IEnumerable<string> lines)
    {
        Guard.Against.Null(lines);

        var result = new List<IReadOnlyList<HighlightSpan>>();
        var inBlockComment = false;
        foreach (var line in lines)
        {
            var spans = new List<HighlightSpan>();
            HighlightLine(line, spans, ref inBlockComment);
            result.Add(spans);
        }

        return result;
    }

    private void HighlightLine(string line, List<HighlightSpan> spans, ref bool inBlockComment)
    {
        var pos = 0;
        if (inBlockComment)
        {
            pos = CloseBlockComment(line, 0, spans, ref inBlockComment);
        }

        while (pos < line.Length)
        {
            var c = line[pos];

            if (IsLineComment(line, pos))
            {
                spans.Add(new(pos, line.Length - pos, CommentColor, CellStyle.Italic));
                return;
            }

            if (Language.BlockCommentStart is { } open && Matches(line, pos, open))
            {
                inBlockComment = true;
                pos = CloseBlockComment(line, pos, spans, ref inBlockComment, open.Length);
                continue;
            }

            if (Language.StringQuotes.Contains(c))
            {
                pos = ScanString(line, pos, spans);
                continue;
            }

            if (Language.VariablePrefix == c && TryScanVariable(line, pos, out var variableEnd))
            {
                spans.Add(new(pos, variableEnd - pos, VariableColor, CellStyle.None));
                pos = variableEnd;
                continue;
            }

            if (char.IsAsciiDigit(c))
            {
                var end = pos + 1;
                while (end < line.Length && (char.IsAsciiLetterOrDigit(line[end]) || line[end] is '_' or '.'))
                {
                    end++;
                }

                spans.Add(new(pos, end - pos, NumberColor, CellStyle.None));
                pos = end;
                continue;
            }

            if (IsWordStart(c))
            {
                var end = pos + 1;
                while (end < line.Length && IsWordPart(line[end]))
                {
                    end++;
                }

                if (Language.Keywords.Contains(line[pos..end]))
                {
                    spans.Add(new(pos, end - pos, KeywordColor, CellStyle.None));
                }

                pos = end;
                continue;
            }

            pos++;
        }
    }

    /// <summary>
    /// Emits the comment from <paramref name="start"/> up to and including the closing
    /// delimiter — or to the line end when it is missing, in which case the comment
    /// continues on the next line. Returns the position after the comment.
    /// </summary>
    private int CloseBlockComment(string line, int start, List<HighlightSpan> spans,
        ref bool inBlockComment, int searchOffset = 0)
    {
        var close = Language.BlockCommentEnd ?? "";
        var index = close.Length == 0
            ? -1
            : line.IndexOf(close, start + searchOffset, StringComparison.Ordinal);
        var end = index < 0 ? line.Length : index + close.Length;
        if (end > start)
        {
            spans.Add(new(start, end - start, CommentColor, CellStyle.Italic));
        }

        inBlockComment = index < 0;
        return end;
    }

    /// <summary>
    /// A line comment starts at its prefix — for '#' only at the line start or after
    /// whitespace, so shell constructs like <c>${#list}</c> or <c>a#b</c> stay code.
    /// </summary>
    private bool IsLineComment(string line, int pos)
    {
        if (Language.LineComment is not { } prefix || !Matches(line, pos, prefix))
        {
            return false;
        }

        return prefix != "#" || pos == 0 || char.IsWhiteSpace(line[pos - 1]);
    }

    /// <summary>Scans a quoted literal; an unterminated one runs to the line end.</summary>
    private int ScanString(string line, int pos, List<HighlightSpan> spans)
    {
        var quote = line[pos];
        var end = pos + 1;
        while (end < line.Length && line[end] != quote)
        {
            end += line[end] == '\\' ? 2 : 1;
        }

        end = Math.Min(end + 1, line.Length);

        var color = StringColor;
        if (Language.HighlightObjectKeys)
        {
            var next = end;
            while (next < line.Length && line[next] == ' ')
            {
                next++;
            }

            if (next < line.Length && line[next] == ':')
            {
                color = KeyColor;
            }
        }

        spans.Add(new(pos, end - pos, color, CellStyle.None));
        return end;
    }

    /// <summary>$name, ${…} and the special parameters $1, $?, $@, $#, $*, $$, $!, $-.</summary>
    private static bool TryScanVariable(string line, int pos, out int end)
    {
        end = pos + 1;
        if (end >= line.Length)
        {
            return false;
        }

        var c = line[end];
        if (c == '{')
        {
            var close = line.IndexOf('}', end);
            end = close < 0 ? line.Length : close + 1;
            return true;
        }

        if (char.IsAsciiDigit(c) || c is '?' or '@' or '#' or '*' or '$' or '!' or '-')
        {
            end++;
            return true;
        }

        if (!IsWordStart(c))
        {
            return false;
        }

        while (end < line.Length && IsWordPart(line[end]))
        {
            end++;
        }

        return true;
    }

    private static bool IsWordStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsWordPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private static bool Matches(string line, int pos, string token)
    {
        return pos + token.Length <= line.Length
            && string.CompareOrdinal(line, pos, token, 0, token.Length) == 0;
    }
}
