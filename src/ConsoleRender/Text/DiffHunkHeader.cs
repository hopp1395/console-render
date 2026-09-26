namespace ConsoleRender;

/// <summary>
/// The numbers of a "@@ -a,b +c,d @@" line. <see cref="End"/> is the index just past the
/// closing "@@", where git puts the enclosing function as context.
/// </summary>
internal readonly record struct DiffHunkHeader(int OldStart, int OldCount, int NewStart, int NewCount, int End);
