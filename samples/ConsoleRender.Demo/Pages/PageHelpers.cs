namespace ConsoleRender.Demo.Pages;

/// <summary>Layout helpers shared by every feature page, keeping their styling consistent.</summary>
internal static class PageHelpers
{
    public static Panel Fill(params Control[] children)
    {
        var panel = new Panel { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        panel.AddRange(children);
        return panel;
    }

    public static Label Info(int top, string text, Color? color = null)
    {
        return new(text) { Left = 0, Top = top, Foreground = color ?? Color.Gray };
    }

    public static Label Section(int top, string text)
    {
        return new(text) { Left = 0, Top = top, Foreground = Color.Yellow, Style = CellStyle.Bold };
    }
}
