namespace ConsoleRender.Demo;

/// <summary>
/// The "Git Changes" page body: a <see cref="GitChangesView"/> over the repository the demo
/// was started in (or the one chosen with /git). It re-reads the repository every few
/// seconds while visible — only visible controls receive Update — and on F5.
/// </summary>
internal sealed class GitPanel : Panel
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(3);

    private readonly IGitChangesSource git;
    private TimeSpan sinceRefresh;

    public GitPanel(IGitChangesSource git, string folder)
    {
        this.git = Guard.Against.Null(git);
        Folder = Guard.Against.NullOrWhiteSpace(folder);
        View = new GitChangesView { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        Add(View);
    }

    public string Folder { get; private set; }

    public GitChangesView View { get; }

    public void Refresh()
    {
        View.Changes = git.Read(Folder);
        sinceRefresh = TimeSpan.Zero;
    }

    public void ShowFolder(string folder)
    {
        Folder = Guard.Against.NullOrWhiteSpace(folder);
        Refresh();
    }

    public override void Update(TimeSpan delta)
    {
        sinceRefresh += delta;
        if (sinceRefresh >= RefreshInterval)
        {
            Refresh();
        }
    }
}
