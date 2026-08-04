using ConsoleRender;

namespace ConsoleRender.Demo;

/// <summary>
/// Showcase application for the ConsoleRender framework. Every control type is on screen
/// at once so the layout, the animations and the input handling can be judged in one look.
/// </summary>
internal static class Program
{
    private const string Logo = """
          ____                      _
         / ___|___  _ __  ___  ___ | | ___
        | |   / _ \| '_ \/ __|/ _ \| |/ _ \
        | |__| (_) | | | \__ \ (_) | |  __/
         \____\___/|_| |_|___/\___/|_|\___|
              R  E  N  D  E  R
        """;

    private static void Main(string[] args)
    {
        Guard.Against.Null(args);

        using var app = new ConsoleApp();
        var ui = BuildUi(app);
        WireCommands(app, ui);
        WireKeyBindings(app, ui);

        ui.Output.AppendLine("Willkommen bei ConsoleRender!", Color.Cyan);
        ui.Output.AppendLine("");
        ui.Output.AppendLine("Tab wechselt den Fokus, F1 zeigt die Hilfe, Strg+Q beendet.", Color.Gray);
        ui.Output.AppendLine("Tippe /help für alle Befehle.", Color.Gray);
        ui.Output.AppendLine("");

        app.SetFocus(ui.Input);

        // "--snapshot [Breite] [Höhe]" renders a single frame as plain text and exits,
        // which makes the layout verifiable without an interactive terminal.
        if (args.Length > 0 && args[0] == "--snapshot")
        {
            int width = args.Length > 1 ? int.Parse(args[1]) : 120;
            int height = args.Length > 2 ? int.Parse(args[2]) : 32;
            ui.ApplyResponsiveLayout(width);
            if (args.Contains("--modal"))
                app.ShowInfo("Information", "Eine modale Infobox. Der Hintergrund wird abgedunkelt.");
            Console.WriteLine(app.RenderOffscreen(width, height).ToText());
            return;
        }

        app.Run();

        Console.WriteLine("ConsoleRender-Demo beendet.");
    }

    /// <summary>The controls the command handlers need to reach after construction.</summary>
    private sealed record Ui(
        OutputField Output,
        CommandInput Input,
        AsciiArt Art,
        Label Status,
        Spinner Spinner,
        SelectMenu Menu,
        RadioGroup BorderChoice,
        Checkbox TypewriterOption,
        IReadOnlyList<Frame> Frames,
        Action<int> ApplyResponsiveLayout);

    private static Ui BuildUi(ConsoleApp app)
    {
        var title = new Label("╔═ ConsoleRender ═ TUI-Framework für .NET ═╗")
        {
            Left = 0,
            Right = 0,
            Top = 0,
            Height = 1,
            TextAlign = TextAlignment.Center,
            Effect = TextEffect.Rainbow,
            Style = CellStyle.Bold,
        };

        var spinner = new Spinner { Text = "bereit", Top = 1, Left = 2, Foreground = Color.Green };

        var hint = new Label("F1 Hilfe · Tab Fokus · Strg+Q Ende")
        {
            Top = 1,
            Right = 2,
            Foreground = Color.DarkGray,
            Style = CellStyle.Italic,
        };

        // Three columns between the header (2 rows) and the input area (4 rows).
        var leftFrame = new Frame("Steuerelemente")
        {
            Left = 0, Top = 2, Bottom = 4, Width = 34,
            BorderColor = Color.Blue,
            TitleColor = Color.Cyan,
        };

        var centerFrame = new Frame("Ausgabe")
        {
            Left = 34, Right = 40, Top = 2, Bottom = 4,
            BorderColor = Color.Blue,
            TitleColor = Color.Cyan,
        };

        var rightFrame = new Frame("ASCII-Grafik")
        {
            Right = 0, Top = 2, Bottom = 4, Width = 40,
            BorderColor = Color.Blue,
            TitleColor = Color.Cyan,
        };

        var inputFrame = new Frame("Eingabe")
        {
            Left = 0, Right = 0, Bottom = 1, Height = 3,
            BorderColor = Color.Magenta,
            TitleColor = Color.Magenta,
        };

        // --- left column ---
        var menu = new SelectMenu("Übersicht", "Farben & Effekte", "Layout & Anker", "Zwischenablage", "Über")
        {
            Left = 0, Right = 0, Top = 1, Height = 5,
        };

        var typewriterOption = new Checkbox("Typewriter-Effekt") { Left = 0, Top = 8 };
        var colorOption = new Checkbox("Farbige Ausgabe") { Left = 0, Top = 9, Checked = true };
        var pulseOption = new Checkbox("Statuszeile pulsieren") { Left = 0, Top = 10 };

        var borderChoice = new RadioGroup("Einfach", "Doppelt", "Rund", "Fett")
        {
            Left = 0, Top = 13, Height = 4,
        };

        leftFrame.AddRange(
            new Label("Menü") { Left = 0, Top = 0, Foreground = Color.Yellow, Style = CellStyle.Bold },
            menu,
            new Label("Optionen") { Left = 0, Top = 7, Foreground = Color.Yellow, Style = CellStyle.Bold },
            typewriterOption,
            colorOption,
            pulseOption,
            new Label("Rahmenstil") { Left = 0, Top = 12, Foreground = Color.Yellow, Style = CellStyle.Bold },
            borderChoice);

        // --- center column ---
        var output = new OutputField { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        centerFrame.Add(output);

        // --- right column ---
        var art = new AsciiArt(Logo)
        {
            Foreground = Color.Green,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Middle,
        };
        rightFrame.Add(art);

        // --- input row ---
        var prompt = new Label("›") { Left = 0, Top = 0, Foreground = Color.Magenta, Style = CellStyle.Bold };
        var input = new CommandInput
        {
            Left = 2, Right = 0, Top = 0, Height = 1,
            Placeholder = "Text eingeben oder /befehl – Tab vervollständigt",
        };
        inputFrame.AddRange(prompt, input);

        var status = new Label("Bereit.") { Left = 1, Bottom = 0, Foreground = Color.DarkGray };

        app.Root.AddRange(title, spinner, hint, leftFrame, centerFrame, rightFrame, inputFrame, status);

        var frames = new[] { leftFrame, centerFrame, rightFrame, inputFrame };

        // Drop the side columns on narrow terminals so the output area stays usable.
        void ApplyResponsiveLayout(int width)
        {
            const int LeftWidth = 34;
            const int RightWidth = 40;

            bool showRight = width >= LeftWidth + RightWidth + 30;
            bool showLeft = width >= LeftWidth + 30;

            rightFrame.Visible = showRight;
            leftFrame.Visible = showLeft;
            centerFrame.Left = showLeft ? LeftWidth : 0;
            centerFrame.Right = showRight ? RightWidth : 0;
            hint.Visible = width >= 70;
        }

        app.Tick += _ => ApplyResponsiveLayout(app.Width);

        // --- interactive wiring ---
        typewriterOption.CheckedChanged += on =>
        {
            output.Typewriter = on;
            status.Text = on ? "Typewriter-Effekt an." : "Typewriter-Effekt aus.";
        };

        pulseOption.CheckedChanged += on => status.Effect = on ? TextEffect.Pulse : TextEffect.None;

        colorOption.CheckedChanged += on =>
            output.Foreground = on ? Color.Default : Color.Gray;

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
                frame.Border = style;
            status.Text = $"Rahmenstil: {borderChoice.SelectedItem}";
        };

        menu.ItemActivated += (_, item) =>
        {
            output.AppendLine($"» {item}", Color.Magenta);
            foreach (string line in DescribeTopic(item))
                output.AppendLine("  " + line);
            output.AppendLine("");
        };

        menu.SelectionChanged += index => status.Text = $"Menü: {menu.Items[index]}";

        return new Ui(output, input, art, status, spinner, menu, borderChoice, typewriterOption,
            frames, ApplyResponsiveLayout);
    }

    private static IEnumerable<string> DescribeTopic(string topic) => topic switch
    {
        "Übersicht" =>
        [
            "Doppelt gepufferter Renderer: jedes Frame wird gegen das",
            "vorherige diffed, es gehen nur geänderte Zellen über die Leitung.",
        ],
        "Farben & Effekte" =>
        [
            "24-Bit-Farben, Stilflags (fett, kursiv, unterstrichen, invers)",
            "und Animationen: Blink, Rainbow, Pulse, Typewriter, Spinner.",
        ],
        "Layout & Anker" =>
        [
            "Anker wie bei CSS: Left/Top/Right/Bottom plus Width/Height.",
            "Beide Anker gesetzt = das Element wächst beim Resize mit.",
            "Ohne Anker greift die Ausrichtung (links, zentriert, rechts).",
        ],
        "Zwischenablage" =>
        [
            "Strg+C und Strg+V im Eingabefeld, /paste holt Text oder",
            "ein Bild aus der Zwischenablage – Bilder werden zu ASCII-Art.",
        ],
        _ =>
        [
            "ConsoleRender 0.1.0 – MIT-Lizenz.",
            "Ein TUI-Framework ohne externe Abhängigkeiten außer GuardClauses.",
        ],
    };

    private static void WireCommands(ConsoleApp app, Ui ui)
    {
        var commands = ui.Input.Commands;

        ui.Input.Submitted += text => ui.Output.AppendLine($"> {text}", Color.White);

        ui.Input.CommandExecuted += result =>
        {
            if (!result.Success)
                ui.Output.AppendLine(result.Message, Color.Red);
        };

        commands.Register("help", "Zeigt alle Befehle und Tastenkürzel", _ =>
        {
            ui.Output.AppendLine("Befehle:", Color.Yellow);
            foreach (var command in commands.All)
                ui.Output.AppendLine($"  /{command.Name,-12} {command.Description}", Color.Gray);
            ui.Output.AppendLine("Tastenkürzel:", Color.Yellow);
            foreach (var binding in app.KeyBindings.All)
                ui.Output.AppendLine($"  {binding.Combo,-12} {binding.Description}", Color.Gray);
            ui.Output.AppendLine("");
        });

        commands.Register("echo", "Gibt den Text farbig aus", args =>
            ui.Output.AppendLine(string.Join(' ', args), Color.Green));

        commands.Register("clear", "Leert das Ausgabefeld", _ =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Ausgabe geleert.";
        });

        commands.Register("color", "Färbt eine Testzeile: /color <name> <text>", args =>
        {
            if (args.Length == 0)
            {
                ui.Output.AppendLine("Verwendung: /color <rot|gruen|blau|gelb|cyan|magenta> [Text]", Color.Yellow);
                return;
            }
            var color = args[0].ToLowerInvariant() switch
            {
                "rot" or "red" => Color.Red,
                "gruen" or "grün" or "green" => Color.Green,
                "blau" or "blue" => Color.Blue,
                "gelb" or "yellow" => Color.Yellow,
                "cyan" => Color.Cyan,
                "magenta" => Color.Magenta,
                _ => Color.White,
            };
            string text = args.Length > 1 ? string.Join(' ', args[1..]) : $"Farbprobe {args[0]}";
            ui.Output.AppendLine(text, color);
        });

        commands.Register("info", "Zeigt eine modale Infobox: /info <Text>", args =>
            app.ShowInfo("Information", args.Length > 0
                ? string.Join(' ', args)
                : "Eine modale Infobox. Der Hintergrund wird abgedunkelt, alle Eingaben gehen an den Dialog."));

        commands.Register("typewriter", "Schaltet den Schreibmaschineneffekt: /typewriter <an|aus>", args =>
        {
            bool on = args.Length == 0 ? !ui.Output.Typewriter : args[0] is "an" or "on" or "1";
            ui.TypewriterOption.Checked = on;
            ui.Output.Typewriter = on;
            ui.Status.Text = on ? "Typewriter-Effekt an." : "Typewriter-Effekt aus.";
        });

        commands.Register("paste", "Fügt Text oder ein Bild aus der Zwischenablage ein", _ =>
        {
            if (Clipboard.TryGetImage(out var image))
            {
                var ascii = AsciiImageConverter.Convert(image, Math.Max(10, ui.Art.Bounds.Width));
                ui.Art.SetImage(ascii);
                ui.Output.AppendLine(
                    $"Bild aus der Zwischenablage übernommen ({image.Width}×{image.Height} Pixel).", Color.Cyan);
                return;
            }

            if (Clipboard.TryGetText(out string clipboardText))
            {
                foreach (string line in clipboardText.Replace("\r", "").Split('\n'))
                    ui.Output.AppendLine(line, Color.Cyan);
                return;
            }

            ui.Output.AppendLine("Die Zwischenablage enthält weder Text noch ein Bild.", Color.Yellow);
        });

        commands.Register("logo", "Stellt die ASCII-Grafik auf das Logo zurück", _ =>
        {
            ui.Art.SetText(Logo);
            ui.Status.Text = "Logo wiederhergestellt.";
        });

        commands.Register("copy", "Kopiert die letzte Eingabe in die Zwischenablage: /copy <Text>", args =>
        {
            string text = string.Join(' ', args);
            Guard.Against.NullOrWhiteSpace(text, nameof(args));
            ui.Output.AppendLine(Clipboard.TrySetText(text)
                ? "In die Zwischenablage kopiert."
                : "Zugriff auf die Zwischenablage fehlgeschlagen.", Color.Cyan);
        });

        commands.Register("busy", "Schaltet den Spinner um", _ =>
        {
            ui.Spinner.Active = !ui.Spinner.Active;
            ui.Spinner.Text = ui.Spinner.Active ? "arbeitet…" : "fertig";
        });

        commands.Register("exit", "Beendet die Demo", _ => app.Exit());
    }

    private static void WireKeyBindings(ConsoleApp app, Ui ui)
    {
        app.KeyBindings.Register(ConsoleKey.F1, "Hilfe anzeigen", () =>
            app.ShowInfo("Tastenkürzel", string.Join('\n', new[]
            {
                "Tab / Shift+Tab   Fokus wechseln",
                "Pfeiltasten       Auswahl bewegen",
                "Leertaste         Umschalten / auswählen",
                "Bild auf/ab       Ausgabe scrollen",
                "Strg+C / Strg+V   Kopieren / Einfügen",
                "Strg+L            Ausgabe leeren",
                "Strg+Q            Beenden",
                "",
                "Befehle beginnen mit /  — Tab vervollständigt sie.",
            })));

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Beenden", app.Exit);

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.L), "Ausgabe leeren", () =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Ausgabe geleert.";
        });
    }
}
