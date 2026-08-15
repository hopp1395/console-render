using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Shows all five <see cref="BorderStyle"/> values side by side.</summary>
internal static class BorderPage
{
    public static Panel Build()
    {
        var styles = new (string Name, BorderStyle Style)[]
        {
            ("Simple", BorderStyle.Single),
            ("Double", BorderStyle.Double),
            ("Rounded", BorderStyle.Rounded),
            ("Thick", BorderStyle.Thick),
            ("Ascii", BorderStyle.Ascii),
        };

        var page = new Panel { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        page.Add(Info(0, "Frames carry a title and one of five border styles."));
        for (var i = 0; i < styles.Length; i++)
        {
            var frame = new Frame(styles[i].Name)
            {
                Left = i % 3 * 18, Top = 2 + i / 3 * 6, Width = 16, Height = 5,
                Border = styles[i].Style,
                BorderColor = Color.Cyan,
            };

            frame.Add(new Label("Content") { Left = 1, Top = 1, Foreground = Color.Gray });
            page.Add(frame);
        }

        return page;
    }
}
