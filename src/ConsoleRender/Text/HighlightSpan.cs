namespace ConsoleRender;

/// <summary>
/// A styled section within one line of text, produced by an <see cref="ISyntaxHighlighter"/>.
///
/// The spans of a line are sorted by <see cref="Start"/> and never overlap. Nested
/// emphasis (bold inside italic, say) is expressed as adjacent spans whose
/// <see cref="CellStyle"/> flags are OR-ed together, which keeps drawing trivial.
/// A <see cref="Color.Default"/> foreground means: keep the control's text color and apply
/// only the style flags.
/// </summary>
public readonly record struct HighlightSpan(int Start, int Length, Color Foreground, CellStyle Style);
