namespace ConsoleRender;

/// <summary>
/// The changes to one file. A path is empty when the diff does not name it; a created or
/// deleted file has "/dev/null" on the missing side, exactly as git writes it.
/// </summary>
internal sealed record DiffFile(string OldPath, string NewPath, IReadOnlyList<DiffHunk> Hunks)
{
    /// <summary>The name to show: the new path, the old one for deletions, "old → new" for renames.</summary>
    public string Title
    {
        get
        {
            if (NewPath is "" or "/dev/null")
            {
                return OldPath;
            }

            if (OldPath is "" or "/dev/null" || OldPath == NewPath)
            {
                return NewPath;
            }

            return $"{OldPath} → {NewPath}";
        }
    }
}
