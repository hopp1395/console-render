namespace ConsoleRender;

/// <summary>
/// Computes styling for a document, used by <see cref="TextArea"/> to color its content.
///
/// The interface deliberately takes the whole document rather than single lines: constructs
/// like fenced code blocks make a line's meaning depend on the lines above it, so only a
/// full pass can classify correctly. Documents edited in a terminal are small; one pass per
/// edit is cheap, and <see cref="TextArea"/> caches the result between edits.
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>
    /// Returns one span list per line, index-parallel to <paramref name="lines"/>. Each
    /// line's spans are sorted by start and free of overlaps (see <see cref="HighlightSpan"/>).
    /// </summary>
    IReadOnlyList<IReadOnlyList<HighlightSpan>> Highlight(IReadOnlyList<string> lines);
}
