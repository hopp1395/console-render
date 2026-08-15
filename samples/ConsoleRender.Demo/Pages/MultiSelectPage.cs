using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A <see cref="MultiSelectMenu"/> over a handful of demo toggles.</summary>
internal static class MultiSelectPage
{
    public static Panel Build(Label status)
    {
        var menu = new MultiSelectMenu(
            "Colored output", "Typewriter effect", "Rainbow title", "Indeterminate progress")
        {
            Left = 0, Top = 3, Right = 0, Height = 5,
        };

        menu.ItemCheckedChanged += (index, isChecked) =>
            status.Text = $"{menu.Items[index]}: {(isChecked ? "checked" : "unchecked")}";

        menu.Submitted += indices => status.Text = indices.Count == 0
            ? "Submitted: nothing checked."
            : $"Submitted: {string.Join(", ", indices.OrderBy(i => i).Select(i => menu.Items[i]))}";

        return Fill(
            Info(0, "Space toggles the highlighted item, Enter submits the checked ones."),
            Info(1, "Moving the cursor alone changes nothing – the same as RadioGroup."),
            menu);
    }
}
