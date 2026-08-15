using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A <see cref="TabControl"/> with a plain, a form and an accent-colored tab.</summary>
internal static class TabsPage
{
    public static Panel Build(out TabControl tabs)
    {
        tabs = new TabControl { Left = 0, Top = 3, Right = 0, Bottom = 0 };

        tabs.AddTab("Overview", Fill(
            new Label("This tab shows only text – no focusable content behind it.")
            {
                Left = 0, Top = 0, Foreground = Color.Gray,
            }));

        var name = new TextBox { Left = 0, Top = 0, Right = 0, Placeholder = "Enter name…" };
        var subscribe = new Checkbox("Subscribe to newsletter") { Left = 0, Top = 2 };
        tabs.AddTab("Form", Fill(name, subscribe));

        tabs.AddTab("Warning", Fill(
            new Label("A tab can override its accent color individually (red here).")
            {
                Left = 0, Top = 0, Foreground = Color.Gray,
            }), accentColor: Color.Red);

        return Fill(
            Info(0, "Arrow keys or Home/End switch the active tab."),
            Info(1, "Tab jumps from the tab header into its content, Shift+Tab back."),
            tabs);
    }
}
