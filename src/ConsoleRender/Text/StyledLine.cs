namespace ConsoleRender;

/// <summary>
/// One finished screen row of a rendered document: its text, the spans that style it and
/// an optional background for the whole row (<see cref="Color.Default"/> for none).
/// </summary>
internal sealed record StyledLine(string Text, IReadOnlyList<HighlightSpan> Spans, Color LineBackground);
