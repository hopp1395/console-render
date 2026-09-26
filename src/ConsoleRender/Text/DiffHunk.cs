namespace ConsoleRender;

/// <summary>A hunk: its "@@ -a,b +c,d @@" header line and the lines below it.</summary>
internal sealed record DiffHunk(string Header, IReadOnlyList<DiffLine> Lines);
