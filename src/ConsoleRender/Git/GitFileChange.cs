namespace ConsoleRender;

/// <summary>
/// One changed file of a working tree: its path relative to the repository root, its line
/// counts, whether it is new (untracked) and whether it is binary — or too large to diff,
/// in which case both counts are 0.
/// </summary>
public sealed record GitFileChange(string Path, int Added, int Removed, bool IsNew, bool IsBinary);
