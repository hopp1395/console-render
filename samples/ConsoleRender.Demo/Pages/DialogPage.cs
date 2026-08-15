using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Buttons opening the info box, the confirmation dialog and the modal editor.</summary>
internal static class DialogPage
{
    public static Panel Build(ConsoleApp app, Label status)
    {
        var info = new Button("Show info box") { Left = 0, Top = 3 };
        info.Clicked += () => app.ShowInfo("Information",
            "A modal info box. The background is dimmed, all input goes to the dialog.");

        var confirm = new Button("Show confirmation") { Left = 0, Top = 5 };
        confirm.Clicked += () => app.ShowConfirm("Confirm", "Save changes before quitting?",
            ["Save", "Discard", "Cancel"],
            (_, label) => status.Text = $"Chosen: {label}");

        var editor = new Button("Open Markdown editor") { Left = 0, Top = 7 };
        editor.Clicked += () => DemoActions.ShowEditorDialog(app, status);

        return Fill(
            Info(0, "Modal dialogs lie on top of the UI; Escape closes them."),
            Info(1, "Buttons respond to Enter or Space."),
            info, confirm, editor);
    }
}
