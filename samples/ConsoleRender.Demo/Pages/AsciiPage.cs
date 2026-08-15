using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>ASCII art rendering plus clipboard paste/restore buttons.</summary>
internal static class AsciiPage
{
    public static Panel Build(AsciiArt art, OutputField output, Label status)
    {
        var paste = new Button("Paste clipboard") { Left = 0, Bottom = 0 };
        paste.Clicked += () => DemoActions.PasteClipboard(art, output, status);

        var reset = new Button("Restore logo") { Left = 27, Bottom = 0 };
        reset.Clicked += () =>
        {
            art.SetText(DemoContent.Logo);
            status.Text = "Logo restored.";
        };

        return Fill(
            Info(0, "ASCII art, single-color or as a colored character grid."),
            Info(1, "An image from the clipboard becomes ASCII art (/paste)."),
            art, paste, reset);
    }
}
