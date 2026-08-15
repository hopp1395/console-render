using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Menu, checkboxes and a radio group that switches the border style live.</summary>
internal static class ChoicesPage
{
    public static Panel Build(Label status, Frame[] frames, CommandInput input, TabControl tabs)
    {
        var menu = new SelectMenu("Overview", "Colors & Effects", "Layout & Anchors", "Clipboard", "About")
        {
            Left = 0, Top = 3, Width = 30, Height = 5,
        };

        menu.SelectionChanged += index => status.Text = $"Menu: {menu.Items[index]}";

        var option1 = new Checkbox("Colored output") { Left = 0, Top = 10, Checked = true };
        var option2 = new Checkbox("Pulse the status line") { Left = 0, Top = 11 };
        option2.CheckedChanged += on => status.Effect = on ? TextEffect.Pulse : TextEffect.None;

        var borderChoice = new RadioGroup("Simple", "Double", "Rounded", "Thick")
        {
            Left = 0, Top = 14, Height = 4,
        };

        borderChoice.SelectionChanged += index =>
        {
            var style = index switch
            {
                1 => BorderStyle.Double,
                2 => BorderStyle.Rounded,
                3 => BorderStyle.Thick,
                _ => BorderStyle.Single,
            };

            foreach (var frame in frames)
            {
                frame.Border = style;
            }

            input.Border = style;
            tabs.Border = style;
            status.Text = $"Border style: {borderChoice.SelectedItem}";
        };

        return Fill(
            Info(0, "Menus, checkboxes and radio groups – space toggles them."),
            Section(2, "Menu"), menu,
            Section(9, "Options"), option1, option2,
            Section(13, "Border style (applies immediately)"), borderChoice);
    }
}
