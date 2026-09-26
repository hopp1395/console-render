namespace ConsoleRender;

/// <summary>
/// Draws one line of text together with its <see cref="HighlightSpan"/> list — the shared
/// drawing step of every control that shows highlighted text.
/// </summary>
internal static class SpanPainter
{
    /// <summary>
    /// Writes <paramref name="line"/> at (<paramref name="x"/> − <paramref name="scrollX"/>,
    /// <paramref name="y"/>) in the base colors, then repaints each span on top of it. A
    /// <see cref="Color.Default"/> span foreground keeps <paramref name="foreground"/>.
    /// Spans outside the visible columns are skipped; the buffer's clip region does the rest.
    /// </summary>
    public static void DrawLine(ConsoleBuffer buffer, int x, int y, int width, string line,
        IReadOnlyList<HighlightSpan>? spans, Color foreground, Color background, int scrollX)
    {
        buffer.Write(x - scrollX, y, line, foreground, background);

        if (spans is null)
        {
            return;
        }

        foreach (var span in spans)
        {
            if (span.Start >= line.Length
                || span.Start + span.Length <= scrollX
                || span.Start >= scrollX + width)
            {
                continue;
            }

            buffer.Write(x + span.Start - scrollX, y,
                line.Substring(span.Start, Math.Min(span.Length, line.Length - span.Start)),
                span.Foreground.IsDefault ? foreground : span.Foreground,
                background, span.Style);
        }
    }
}
