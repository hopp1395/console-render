namespace ConsoleRender.Demo;

/// <summary>
/// Showcase application for the ConsoleRender framework, laid out as a feature gallery:
/// the list on the left selects a feature, the panel on the right presents it. Pages are
/// built once and swapped in and out, so their state survives switching back and forth.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        Guard.Against.Null(args);

        using var app = new ConsoleApp();
        var ui = UiBuilder.Build(app);
        DemoCommands.Wire(app, ui);
        DemoKeyBindings.Wire(app, ui);
        DemoCommands.FillHelpPage(app, ui);

        app.SetFocus(ui.Input);

        // "--snapshot [Breite] [Höhe]" renders a single frame as plain text and exits,
        // which makes the layout verifiable without an interactive terminal.
        if (args.Length > 0 && args[0] == "--snapshot")
        {
            var width = args.Length > 1 ? int.Parse(args[1]) : 120;
            var height = args.Length > 2 ? int.Parse(args[2]) : 32;
            ui.ApplyResponsiveLayout(width);

            // "--run /befehl" führt einen Befehl aus, bevor das Bild gerendert wird.
            var runIndex = Array.IndexOf(args, "--run");
            if (runIndex >= 0 && runIndex + 1 < args.Length)
            {
                ui.Input.Commands.Execute(args[runIndex + 1]);
            }

            if (args.Contains("--modal"))
            {
                app.ShowInfo("Information", "A modal info box. The background is dimmed.");
            }

            if (args.Contains("--confirm"))
            {
                app.ShowConfirm("Quit", "Really quit the demo?", ["Quit", "Cancel"], (_, _) => { });
            }

            Console.WriteLine(app.RenderOffscreen(width, height).ToText());
            return;
        }

        app.Run();

        Console.WriteLine("ConsoleRender demo exited.");
    }
}
