namespace ConsoleRender.Demo;

/// <summary>Registers every slash command the demo understands, and builds the help page text.</summary>
internal static class DemoCommands
{
    public static void Wire(ConsoleApp app, Ui ui)
    {
        var commands = ui.Input.Commands;

        ui.Input.Submitted += text => ui.Output.AppendLine($"> {text}", Color.White);

        ui.Input.CommandExecuted += result =>
        {
            if (!result.Success)
            {
                ui.Output.AppendLine(result.Message, Color.Red);
            }
        };

        commands.Register("help", "Shows commands and shortcuts", _ =>
        {
            FillHelpPage(app, ui);
            ui.ShowFeature("Commands & Shortcuts");
        });

        commands.Register("feature", "Switches the feature: /feature <name>", args =>
        {
            var query = string.Join(' ', args);
            Guard.Against.NullOrWhiteSpace(query, nameof(args));
            var match = ui.FeatureNames
                .FirstOrDefault(n => n.Contains(query, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                ui.Status.Text = $"No feature matches \"{query}\".";
                return;
            }

            ui.ShowFeature(match);
        });

        commands.Register("echo", "Prints the text to the output log", args =>
        {
            ui.Output.AppendLine(string.Join(' ', args), Color.Green);
            ui.ShowFeature("Output Log & Task Lines");
        });

        commands.Register("clear", "Clears the output log", _ =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Output cleared.";
        });

        commands.Register("color", "Colors a test line: /color <name> <text>", args =>
        {
            if (args.Length == 0)
            {
                ui.Output.AppendLine("Usage: /color <red|green|blue|yellow|cyan|magenta> [text]", Color.Yellow);
                return;
            }

            var color = args[0].ToLowerInvariant() switch
            {
                "red" => Color.Red,
                "green" => Color.Green,
                "blue" => Color.Blue,
                "yellow" => Color.Yellow,
                "cyan" => Color.Cyan,
                "magenta" => Color.Magenta,
                _ => Color.White,
            };

            var text = args.Length > 1 ? string.Join(' ', args[1..]) : $"Color sample {args[0]}";
            ui.Output.AppendLine(text, color);
            ui.ShowFeature("Output Log & Task Lines");
        });

        commands.Register("typewriter", "Toggles the typewriter effect: /typewriter <on|off>", args =>
        {
            var on = args.Length == 0 ? !ui.Output.Typewriter : args[0] is "on" or "1";
            ui.TypewriterOption.Checked = on;
            ui.Output.Typewriter = on;
            ui.Status.Text = on ? "Typewriter effect on." : "Typewriter effect off.";
        });

        commands.Register("task", "Simulates a task with a task line: /task [seconds]", args =>
        {
            var seconds = args.Length > 0 ? double.Parse(args[0]) : 3;
            Guard.Against.NegativeOrZero(seconds, nameof(args));
            DemoActions.RunDemoTask(app, ui.Output, seconds, ui.Progress);
            ui.ShowFeature("Output Log & Task Lines");
        });

        commands.Register("progress", "Sets the progress bar: /progress <0-100|indeterminate>", args =>
        {
            if (args.Length == 0 || args[0] is "indeterminate" or "marquee")
            {
                ui.Progress.Indeterminate = !ui.Progress.Indeterminate;
                ui.Status.Text = ui.Progress.Indeterminate
                    ? "Progress: indeterminate."
                    : "Progress: determinate.";
            }
            else
            {
                ui.Progress.Indeterminate = false;
                ui.Progress.Value = double.Parse(args[0]);
                ui.Status.Text = $"Progress: {ui.Progress.Value:0} %";
            }

            ui.ShowFeature("Progress & Spinner");
        });

        commands.Register("paste", "Pastes text or an image from the clipboard", _ =>
        {
            DemoActions.PasteClipboard(ui.Art, ui.Output, ui.Status);
            ui.ShowFeature("ASCII Art & Clipboard");
        });

        commands.Register("logo", "Resets the ASCII art back to the logo", _ =>
        {
            ui.Art.SetText(DemoContent.Logo);
            ui.Status.Text = "Logo restored.";
            ui.ShowFeature("ASCII Art & Clipboard");
        });

        commands.Register("copy", "Copies text to the clipboard: /copy <text>", args =>
        {
            var text = string.Join(' ', args);
            Guard.Against.NullOrWhiteSpace(text, nameof(args));
            ui.Status.Text = Clipboard.TrySetText(text)
                ? "Copied to the clipboard."
                : "Clipboard access failed.";
        });

        commands.Register("busy", "Toggles the spinner in the header", _ =>
        {
            ui.Spinner.Active = !ui.Spinner.Active;
            ui.Spinner.Text = ui.Spinner.Active ? "working…" : "done";
        });

        commands.Register("border", "Border of the input field: /border <full|lines|none>", args =>
        {
            ui.Input.BorderMode = args.Length == 0
                ? (BorderMode)(((int)ui.Input.BorderMode + 1) % 3)
                : args[0].ToLowerInvariant() switch
                {
                    "full" => BorderMode.Full,
                    "lines" => BorderMode.TopAndBottom,
                    "none" => BorderMode.None,
                    _ => throw new ArgumentException($"Unknown border: {args[0]}"),
                };

            ui.Status.Text = $"Input field border: {ui.Input.BorderMode}";
        });

        commands.Register("info", "Shows a modal info box: /info <text>", args =>
            app.ShowInfo("Information", args.Length > 0
                ? string.Join(' ', args)
                : "A modal info box. The background is dimmed, all input goes to the dialog."));

        commands.Register("confirm", "Shows a confirmation dialog", _ =>
            app.ShowConfirm("Confirm", "Save changes before quitting?",
                ["Save", "Discard", "Cancel"],
                (_, label) => ui.Status.Text = $"Chosen: {label}"));

        commands.Register("editor", "Opens the Markdown editor", _ => DemoActions.ShowEditorDialog(app, ui.Status));

        commands.Register("exit", "Exits the demo", _ => DemoActions.ConfirmExit(app, ui));
    }

    public static void FillHelpPage(ConsoleApp app, Ui ui)
    {
        ui.HelpOutput.Clear();
        ui.HelpOutput.AppendLine("Commands start with / – Tab completes them.", Color.Gray);
        ui.HelpOutput.AppendLine("");
        ui.HelpOutput.AppendLine("Commands:", Color.Yellow);
        foreach (var command in ui.Input.Commands.All)
        {
            ui.HelpOutput.AppendLine($"  /{command.Name,-12} {command.Description}", Color.Gray);
        }

        ui.HelpOutput.AppendLine("");
        ui.HelpOutput.AppendLine("Shortcuts:", Color.Yellow);
        foreach (var binding in app.KeyBindings.All)
        {
            ui.HelpOutput.AppendLine($"  {binding.Combo,-12} {binding.Description}", Color.Gray);
        }

        ui.HelpOutput.AppendLine("  Tab/Shift+Tab  switch focus · arrows move · space toggles", Color.Gray);
    }
}
