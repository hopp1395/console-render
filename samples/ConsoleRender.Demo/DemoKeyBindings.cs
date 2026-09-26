namespace ConsoleRender.Demo;

/// <summary>Registers the demo's global key bindings.</summary>
internal static class DemoKeyBindings
{
    public static void Wire(ConsoleApp app, Ui ui)
    {
        app.KeyBindings.Register(ConsoleKey.F1, "Show help", () =>
        {
            DemoCommands.FillHelpPage(app, ui);
            ui.ShowFeature("Commands & Shortcuts");
        });

        // The modal editor handles F2 itself: with a modal open, global bindings are skipped.
        app.KeyBindings.Register(ConsoleKey.F2, "Markdown: editor / preview / split", () =>
        {
            if (ui.CurrentFeature() != "Markdown Editor")
            {
                ui.ShowFeature("Markdown Editor");
                app.SetFocus(ui.Workbench.Editor);
                return;
            }

            ui.Workbench.CycleMode();
            ui.Status.Text = $"Markdown: {ui.Workbench.Mode}";
        });

        app.KeyBindings.Register(ConsoleKey.F5, "Refresh the Git page", () =>
        {
            if (ui.CurrentFeature() == "Git Changes")
            {
                ui.Git.Refresh();
                ui.Status.Text = "Git changes refreshed.";
            }
        });

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Quit", () => DemoActions.ConfirmExit(app, ui));

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.L), "Clear output", () =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Output cleared.";
        });
    }
}
