namespace ConsoleRender;

/// <summary>
/// A span found by <see cref="MarkdownInlineScanner"/>. Markers are the syntax itself —
/// the asterisks, backticks, brackets and a link's "(url)" — which the editor dims and
/// the preview removes.
/// </summary>
internal readonly record struct MarkdownInlineSpan(int Start, int Length, Color Foreground, CellStyle Style, bool IsMarker)
{
    public static MarkdownInlineSpan Marker(int start, int length)
    {
        return new(start, length, Color.Default, CellStyle.None, true);
    }
}
