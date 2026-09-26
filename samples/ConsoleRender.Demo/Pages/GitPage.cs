using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>The uncommitted changes of a repository: branch, ↑/↓ commits, files and one diff.</summary>
internal static class GitPage
{
    public static Panel Build(out GitPanel panel)
    {
        panel = new GitPanel(new GitClient(), Environment.CurrentDirectory)
        {
            Left = 0, Top = 3, Right = 0, Bottom = 0,
        };

        return Fill(
            Info(0, "Branch, outgoing ↑ / incoming ↓ commits (as of the last fetch), changed files."),
            Info(1, "F5 refreshes, /git <path> shows another repository; Tab here, then N/P jump."),
            panel);
    }
}
