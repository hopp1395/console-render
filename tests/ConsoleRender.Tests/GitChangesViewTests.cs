namespace ConsoleRender.Tests;

public class GitChangesViewTests
{
    private const string Diff = "diff --git a/a.cs b/a.cs\n--- a/a.cs\n+++ b/a.cs\n@@ -1 +1,2 @@\n-old\n+new\n+more\n";

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0')
    {
        return new(ch, key, false, false, false);
    }

    private static GitChanges Changes(int? ahead = 2, int? behind = 1, int extraFiles = 0)
    {
        var files = new List<GitFileChange>
        {
            new("a.cs", 2, 1, IsNew: false, IsBinary: false),
            new("docs/new.md", 3, 0, IsNew: true, IsBinary: false),
            new("logo.png", 0, 0, IsNew: true, IsBinary: true),
        };

        for (var i = 0; i < extraFiles; i++)
        {
            files.Add(new GitFileChange($"extra{i}.txt", 1, 0, IsNew: true, IsBinary: false));
        }

        return new GitChanges("C:/repo", "feature/x", ahead, behind, Diff, files, null);
    }

    private static string[] Rows(GitChangesView view, int width, int height)
    {
        var app = new ConsoleApp();
        view.Parent?.Remove(view);
        view.Left = 0;
        view.Top = 0;
        view.Width = width;
        view.Height = height;
        app.Root.Add(view);
        return app.RenderOffscreen(width, height).ToText().Split('\n').Select(row => row.TrimEnd()).ToArray();
    }

    [Fact]
    public void TheSummaryShowsBranchCommitsTotalsAndFiles()
    {
        var rows = Rows(new GitChangesView { Changes = Changes(), Layout = DiffLayout.Unified }, 80, 12);

        Assert.Equal("feature/x  ↑2 ↓1  3 files changed  +5 −1", rows[0]);
        // Bars are scaled to the largest change (3 lines = 20 cells).
        Assert.Equal(" a.cs            +2    −1 " + new string('+', 14) + new string('-', 7), rows[1]);
        Assert.Equal(" docs/new.md     +3    −0 " + new string('+', 20) + " new", rows[2]);
        Assert.Equal(" logo.png    new, binary or large", rows[3]);
        Assert.Equal("", rows[4]);
        Assert.StartsWith("─ a.cs ─", rows[5]);
    }

    [Fact]
    public void WithoutAnUpstreamAHintReplacesTheArrows()
    {
        var rows = Rows(new GitChangesView { Changes = Changes(null, null) }, 80, 6);

        Assert.StartsWith("feature/x  no upstream  3 files changed", rows[0]);
    }

    [Fact]
    public void ACleanTreeSaysSo()
    {
        var clean = new GitChanges("C:/repo", "main", 0, 0, "", [], null);

        var rows = Rows(new GitChangesView { Changes = clean }, 60, 4);

        Assert.Equal("main  ↑0 ↓0  working tree clean", rows[0]);
        Assert.Equal("No changes.", rows[2]);
    }

    [Fact]
    public void FilesBeyondTheRowLimitAreSummedUp()
    {
        var view = new GitChangesView { Changes = Changes(extraFiles: 5), MaxSummaryRows = 4 };

        var rows = Rows(view, 80, 10);

        Assert.Equal(" … 6 more files", rows[3]);
    }

    [Fact]
    public void AnErrorIsShownInsteadOfTheSummary()
    {
        var view = new GitChangesView { Changes = GitChanges.Failed("Not a git repository: C:/x") };

        var rows = Rows(view, 60, 3);

        Assert.Equal("Not a git repository: C:/x", rows[0]);
        Assert.False(view.OnKey(Key(ConsoleKey.DownArrow)));
    }

    [Fact]
    public void BeforeTheFirstReadAHintIsShown()
    {
        Assert.Equal("No repository read yet.", Rows(new GitChangesView(), 40, 2)[0]);
    }

    [Fact]
    public void KeysScrollTheDiffAndTheSameDiffKeepsThePosition()
    {
        var view = new GitChangesView { Changes = Changes(), Layout = DiffLayout.Unified };
        Rows(view, 60, 8);

        Assert.True(view.OnKey(Key(ConsoleKey.DownArrow)));
        var scrolled = Rows(view, 60, 8);
        Assert.StartsWith("@@ -1 +1,2 @@", scrolled[5]);

        // A refresh that reads the same diff must not jump back to the top.
        view.Changes = Changes();
        Assert.StartsWith("@@ -1 +1,2 @@", Rows(view, 60, 8)[5]);
    }

    [Fact]
    public void TheViewIsASingleTabStop()
    {
        var app = new ConsoleApp();
        var view = new GitChangesView { Left = 0, Top = 0, Width = 40, Height = 5, Changes = Changes() };
        app.Root.Add(view);

        app.CycleFocus();

        Assert.Same(view, app.FocusedControl);
        app.CycleFocus();
        Assert.Same(view, app.FocusedControl);
    }
}
