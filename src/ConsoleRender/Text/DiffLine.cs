namespace ConsoleRender;

/// <summary>
/// One line of a hunk, without its '+'/'-'/' ' prefix. The numbers are the line's position
/// in the old and new file; a side the line does not exist on is null.
/// </summary>
internal sealed record DiffLine(DiffLineKind Kind, string Text, int? OldNumber, int? NewNumber);
