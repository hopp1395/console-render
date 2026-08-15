using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>Progress bar controls plus a standalone spinner toggle.</summary>
internal static class ProgressPage
{
    public static Panel Build(ProgressBar progress, Label status)
    {
        var plus = new Button("+10 %") { Left = 0, Top = 5 };
        plus.Clicked += () => progress.Value = Math.Min(100, progress.Value + 10);
        var minus = new Button("−10 %") { Left = 10, Top = 5 };
        minus.Clicked += () => progress.Value = Math.Max(0, progress.Value - 10);

        var endless = new Checkbox("Indeterminate animation") { Left = 22, Top = 5 };
        endless.CheckedChanged += on =>
        {
            progress.Indeterminate = on;
            status.Text = on ? "Progress: indeterminate." : "Progress: determinate.";
        };

        var spinner = new Spinner { Left = 0, Top = 8, Text = "working…", Foreground = Color.Cyan };
        var spinnerActive = new Checkbox("Spinner active") { Left = 20, Top = 8, Checked = true };
        spinnerActive.CheckedChanged += on => spinner.Active = on;

        return Fill(
            Info(0, "The bar fills in eighth-cell steps; /progress and /task control it."),
            progress, plus, minus, endless,
            spinner, spinnerActive);
    }
}
