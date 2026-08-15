using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Single-line text inputs with the three border modes.</summary>
internal static class TextBoxPage
{
    public static Panel Build()
    {
        var plain = new TextBox { Left = 0, Top = 3, Right = 0, Height = 1, Placeholder = "no border" };
        var lines = new TextBox
        {
            Left = 0, Top = 6, Right = 0, Height = 3,
            BorderMode = BorderMode.TopAndBottom, BorderColor = Color.Magenta,
            Placeholder = "lines above and below",
        };

        var full = new TextBox
        {
            Left = 0, Top = 10, Right = 0, Height = 3,
            BorderMode = BorderMode.Full, BorderColor = Color.Cyan,
            Placeholder = "full border",
        };

        return Fill(
            Info(0, "Single-line input with cursor, scrolling and clipboard support"),
            Info(1, "(Ctrl+C copies, Ctrl+V pastes, Ctrl+U clears)."),
            plain, lines, full);
    }
}
