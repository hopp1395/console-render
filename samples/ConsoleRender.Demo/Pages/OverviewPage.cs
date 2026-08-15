using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Landing page: the logo plus a short orientation text.</summary>
internal static class OverviewPage
{
    public static Panel Build()
    {
        var logo = new AsciiArt(DemoContent.Logo)
        {
            Foreground = Color.Green,
            HorizontalAlignment = HorizontalAlignment.Center,
            Top = 2,
        };

        return Fill(
            logo,
            new Label("A double-buffered TUI framework for .NET.")
            {
                Left = 0, Right = 0, Top = 10, TextAlign = TextAlignment.Center, Foreground = Color.White,
            },
            new Label("Pick a feature on the left – arrow keys move, typing filters.")
            {
                Left = 0, Right = 0, Top = 12, TextAlign = TextAlignment.Center, Foreground = Color.Gray,
            },
            new Label("Tab switches focus, F1 shows help, /help lists all commands.")
            {
                Left = 0, Right = 0, Top = 13, TextAlign = TextAlignment.Center, Foreground = Color.Gray,
            });
    }
}
