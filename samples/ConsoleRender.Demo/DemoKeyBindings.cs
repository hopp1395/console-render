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

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Quit", () => DemoActions.ConfirmExit(app, ui));

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.L), "Clear output", () =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Output cleared.";
        });
    }
}
