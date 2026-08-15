using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A playground demonstrating every anchor combination.</summary>
internal static class LayoutPage
{
    public static Panel Build()
    {
        var playground = new Frame("Playground")
        {
            Left = 0, Top = 4, Right = 0, Bottom = 0,
            BorderColor = Color.DarkGray,
        };

        playground.AddRange(
            new Label("Left=0, Top=0") { Left = 0, Top = 0, Foreground = Color.Cyan },
            new Label("Right=0, Top=0") { Right = 0, Top = 0, Foreground = Color.Cyan },
            new Label("Left=0, Bottom=0") { Left = 0, Bottom = 0, Foreground = Color.Cyan },
            new Label("Right=0, Bottom=0") { Right = 0, Bottom = 0, Foreground = Color.Cyan },
            new Label("centered (no anchors)")
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Foreground = Color.Yellow, Style = CellStyle.Bold,
            },
            new Label("Left=4 and Right=4 set → stretches on resize")
            {
                Left = 4, Right = 4, Top = 2,
                TextAlign = TextAlignment.Center,
                Background = Color.DarkBlue, Foreground = Color.White,
            });
        return Fill(
            Info(0, "Anchors like CSS absolute: Left/Top/Right/Bottom plus Width/Height."),
            Info(1, "Both anchors set = the element grows with the terminal;"),
            Info(2, "without anchors, alignment decides. Resize the window!"),
            playground);
    }
}
