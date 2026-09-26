namespace ConsoleRender;

/// <summary>What a line inside a unified diff hunk stands for.</summary>
internal enum DiffLineKind
{
    Context,
    Added,
    Removed,

    /// <summary>A "\ No newline at end of file" marker; belongs to the line before it.</summary>
    Note,
}
