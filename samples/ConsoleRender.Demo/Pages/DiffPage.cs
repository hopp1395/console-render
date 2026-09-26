using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A side-by-side / unified diff viewer; /diff switches the layout.</summary>
internal static class DiffPage
{
    public static Panel Build(out DiffView view)
    {
        view = new DiffView
        {
            Left = 0, Top = 3, Right = 0, Bottom = 0,
            Diff = DemoContent.SampleDiff,
        };

        return Fill(
            Info(0, "↑↓ PgUp/PgDn scroll, ←→ long lines, N/P jump between hunks."),
            Info(1, "/diff <auto|split|unified> switches the layout."),
            view);
    }
}
