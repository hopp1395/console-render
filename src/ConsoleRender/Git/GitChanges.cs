namespace ConsoleRender;

/// <summary>
/// Everything that changed in a repository since the last commit, as read by
/// <see cref="GitClient"/>: the checked-out branch, its commits ahead of (outgoing) and
/// behind (incoming) its upstream — null without an upstream — one unified diff over all
/// files (untracked ones as added files) and per-file line counts.
/// <see cref="Error"/> is set instead when the folder is no repository or git failed.
/// </summary>
public sealed record GitChanges(
    string Root,
    string Branch,
    int? Ahead,
    int? Behind,
    string Diff,
    IReadOnlyList<GitFileChange> Files,
    string? Error)
{
    public static GitChanges Failed(string error)
    {
        Guard.Against.NullOrWhiteSpace(error);

        return new GitChanges("", "", null, null, "", [], error);
    }

    public int Added => Files.Sum(file => file.Added);

    public int Removed => Files.Sum(file => file.Removed);
}
