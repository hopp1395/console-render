using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Shows label text effects, styles and alignment options.</summary>
internal static class LabelPage
{
    public static Panel Build()
    {
        return Fill(
            Info(0, "Labels show text with color, style and animation."),
            Section(2, "Effects"),
            new Label("No effect – but with color") { Left = 2, Top = 3, Foreground = Color.Cyan },
            new Label("Blink: on and off") { Left = 2, Top = 4, Effect = TextEffect.Blink, Foreground = Color.Yellow },
            new Label("Rainbow: a drifting color gradient") { Left = 2, Top = 5, Effect = TextEffect.Rainbow },
            new Label("Pulse: brightness breathes") { Left = 2, Top = 6, Effect = TextEffect.Pulse, Foreground = Color.Magenta },
            Section(8, "Styles"),
            new Label("bold") { Left = 2, Top = 9, Style = CellStyle.Bold },
            new Label("italic") { Left = 8, Top = 9, Style = CellStyle.Italic },
            new Label("underlined") { Left = 16, Top = 9, Style = CellStyle.Underline },
            new Label("strikethrough") { Left = 31, Top = 9, Style = CellStyle.Strikethrough },
            Section(11, "Alignment"),
            new Label("left-aligned") { Left = 0, Right = 0, Top = 12, TextAlign = TextAlignment.Left },
            new Label("centered") { Left = 0, Right = 0, Top = 13, TextAlign = TextAlignment.Center },
            new Label("right-aligned") { Left = 0, Right = 0, Top = 14, TextAlign = TextAlignment.Right });
    }
}
