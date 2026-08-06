using ConsoleRender;

namespace ConsoleRender.Demo;

/// <summary>
/// Showcase application for the ConsoleRender framework, laid out as a feature gallery:
/// the list on the left selects a feature, the panel on the right presents it. Pages are
/// built once and swapped in and out, so their state survives switching back and forth.
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

    private const string SampleMarkdown = """
        # Markdown-Editor

        Der Editor hebt **fett**, *kursiv*, `code` und ~~gestrichen~~ hervor,
        dazu ***beides zugleich***.

        - Listenpunkt mit [Link](https://example.org)
        1. Nummerierte Liste

        > Zitate erscheinen kursiv und grau.

        ```csharp
        var app = new ConsoleApp();
        app.Run();
        ```

        ---
        Esc schließt den Editor.
        """;

    private static void Main(string[] args)
    {
        Guard.Against.Null(args);

        using var app = new ConsoleApp();
        var ui = BuildUi(app);
        WireCommands(app, ui);
        WireKeyBindings(app, ui);
        FillHelpPage(app, ui);

        app.SetFocus(ui.Input);

        // "--snapshot [Breite] [Höhe]" renders a single frame as plain text and exits,
        // which makes the layout verifiable without an interactive terminal.
        if (args.Length > 0 && args[0] == "--snapshot")
        {
            int width = args.Length > 1 ? int.Parse(args[1]) : 120;
            int height = args.Length > 2 ? int.Parse(args[2]) : 32;
            ui.ApplyResponsiveLayout(width);

            // "--run /befehl" führt einen Befehl aus, bevor das Bild gerendert wird.
            int runIndex = Array.IndexOf(args, "--run");
            if (runIndex >= 0 && runIndex + 1 < args.Length)
                ui.Input.Commands.Execute(args[runIndex + 1]);

            if (args.Contains("--modal"))
                app.ShowInfo("Information", "Eine modale Infobox. Der Hintergrund wird abgedunkelt.");
            if (args.Contains("--confirm"))
                app.ShowConfirm("Beenden", "Demo wirklich beenden?", ["Beenden", "Zurück"], (_, _) => { });
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
        ProgressBar Progress,
        Checkbox TypewriterOption,
        OutputField HelpOutput,
        IReadOnlyList<string> FeatureNames,
        Action<string> ShowFeature,
        Action<int> ApplyResponsiveLayout);

    private static Ui BuildUi(ConsoleApp app)
    {
        var title = new Label("╔═ ConsoleRender ═ TUI-Framework für .NET ═╗")
        {
            Left = 0, Right = 0, Top = 0, Height = 1,
            TextAlign = TextAlignment.Center,
            Effect = TextEffect.Rainbow,
            Style = CellStyle.Bold,
        };

        var spinner = new Spinner { Text = "bereit", Top = 1, Left = 2, Foreground = Color.Green };

        var hint = new Label("F1 Hilfe · Tab Fokus · Strg+Q Ende")
        {
            Top = 1, Right = 2,
            Foreground = Color.DarkGray,
            Style = CellStyle.Italic,
        };

        var leftFrame = new Frame("Features")
        {
            Left = 0, Top = 2, Bottom = 4, Width = 30,
            BorderColor = Color.Blue,
            TitleColor = Color.Cyan,
        };

        var rightFrame = new Frame
        {
            Left = 30, Right = 0, Top = 2, Bottom = 4,
            BorderColor = Color.Blue,
            TitleColor = Color.Cyan,
        };

        // The input draws its own border, so it needs no surrounding frame.
        var input = new CommandInput
        {
            Left = 0, Right = 0, Bottom = 1, Height = 3,
            BorderMode = BorderMode.TopAndBottom,
            BorderColor = Color.Magenta,
            Placeholder = "› Text eingeben oder /befehl – Tab vervollständigt",
        };

        var status = new Label("Bereit.") { Left = 1, Bottom = 0, Foreground = Color.DarkGray };

        app.Root.AddRange(title, spinner, hint, leftFrame, rightFrame, input, status);

        // --- the feature pages, built once so their state survives switching ---
        var output = new OutputField { Left = 0, Top = 3, Right = 0, Bottom = 2 };
        var typewriterOption = new Checkbox("Typewriter") { Right = 0, Bottom = 0 };
        var art = new AsciiArt(Logo)
        {
            Foreground = Color.Green,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Middle,
        };
        var progress = new ProgressBar { Left = 0, Top = 3, Right = 0, Height = 1 };
        var helpOutput = new OutputField { Left = 0, Top = 0, Right = 0, Bottom = 0 };

        var pages = new List<(string Name, Control Page)>
        {
            ("Übersicht", OverviewPage()),
            ("Labels & Effekte", LabelPage()),
            ("Ausgabe-Log & Task-Zeilen", OutputPage(app, output, typewriterOption, status)),
            ("Textfelder", TextBoxPage()),
            ("Markdown-Editor", EditorPage()),
            ("Suchfeld", SearchPage(status)),
            ("Auswahl & Optionen", ChoicesPage(status, [leftFrame, rightFrame], input)),
            ("Fortschritt & Spinner", ProgressPage(progress, status)),
            ("Rahmen & Stile", BorderPage()),
            ("Dialoge & Buttons", DialogPage(app, status)),
            ("ASCII-Grafik & Zwischenablage", AsciiPage(art, output, status)),
            ("Layout & Anker", LayoutPage()),
            ("Befehle & Tastenkürzel", Fill(helpOutput)),
        };
        var pageByName = pages.ToDictionary(p => p.Name, p => p.Page);
        var featureNames = pages.Select(p => p.Name).ToList();

        var nav = new SearchBox(featureNames.ToArray())
        {
            Left = 0, Top = 0, Right = 0, Bottom = 0,
            EmptyText = "keine Treffer",
        };
        nav.Input.Placeholder = "Feature suchen…";
        leftFrame.Add(nav);

        Control? currentPage = null;
        void ShowFeature(string name)
        {
            if (!pageByName.TryGetValue(name, out var page) || ReferenceEquals(page, currentPage))
                return;
            if (currentPage is not null)
                rightFrame.Remove(currentPage);
            rightFrame.Add(page);
            rightFrame.Title = name;
            currentPage = page;
            status.Text = $"Feature: {name}";
        }

        nav.SelectionChanged += (_, name) => ShowFeature(name);
        nav.ItemActivated += (_, name) => ShowFeature(name);
        ShowFeature(featureNames[0]);

        // Drop the feature list on narrow terminals so the presentation stays usable.
        void ApplyResponsiveLayout(int width)
        {
            bool showNav = width >= 64;
            leftFrame.Visible = showNav;
            rightFrame.Left = showNav ? 30 : 0;
            hint.Visible = width >= 70;

            int inputRows = input.BorderMode == BorderMode.None ? 1 : 3;
            input.Height = inputRows;
            leftFrame.Bottom = rightFrame.Bottom = inputRows + 1;
        }

        app.Tick += _ => ApplyResponsiveLayout(app.Width);

        typewriterOption.CheckedChanged += on =>
        {
            output.Typewriter = on;
            status.Text = on ? "Typewriter-Effekt an." : "Typewriter-Effekt aus.";
        };

        return new Ui(output, input, art, status, spinner, progress, typewriterOption,
            helpOutput, featureNames, ShowFeature, ApplyResponsiveLayout);
    }

    // --- page builders -------------------------------------------------------------------

    private static Panel Fill(params Control[] children)
    {
        var panel = new Panel { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        panel.AddRange(children);
        return panel;
    }

    private static Label Info(int top, string text, Color? color = null) =>
        new(text) { Left = 0, Top = top, Foreground = color ?? Color.Gray };

    private static Label Section(int top, string text) =>
        new(text) { Left = 0, Top = top, Foreground = Color.Yellow, Style = CellStyle.Bold };

    private static Panel OverviewPage()
    {
        var logo = new AsciiArt(Logo)
        {
            Foreground = Color.Green,
            HorizontalAlignment = HorizontalAlignment.Center,
            Top = 2,
        };
        return Fill(
            logo,
            new Label("Ein doppelt gepuffertes TUI-Framework für .NET.")
            {
                Left = 0, Right = 0, Top = 10, TextAlign = TextAlignment.Center, Foreground = Color.White,
            },
            new Label("Links ein Feature wählen – Pfeiltasten bewegen, Tippen filtert.")
            {
                Left = 0, Right = 0, Top = 12, TextAlign = TextAlignment.Center, Foreground = Color.Gray,
            },
            new Label("Tab wechselt den Fokus, F1 zeigt die Hilfe, /help alle Befehle.")
            {
                Left = 0, Right = 0, Top = 13, TextAlign = TextAlignment.Center, Foreground = Color.Gray,
            });
    }

    private static Panel LabelPage()
    {
        return Fill(
            Info(0, "Labels zeigen Text mit Farbe, Stil und Animation."),
            Section(2, "Effekte"),
            new Label("Ohne Effekt – aber mit Farbe") { Left = 2, Top = 3, Foreground = Color.Cyan },
            new Label("Blink: an und aus") { Left = 2, Top = 4, Effect = TextEffect.Blink, Foreground = Color.Yellow },
            new Label("Rainbow: wandernder Farbverlauf") { Left = 2, Top = 5, Effect = TextEffect.Rainbow },
            new Label("Pulse: Helligkeit atmet") { Left = 2, Top = 6, Effect = TextEffect.Pulse, Foreground = Color.Magenta },
            Section(8, "Stile"),
            new Label("fett") { Left = 2, Top = 9, Style = CellStyle.Bold },
            new Label("kursiv") { Left = 8, Top = 9, Style = CellStyle.Italic },
            new Label("unterstrichen") { Left = 16, Top = 9, Style = CellStyle.Underline },
            new Label("durchgestrichen") { Left = 31, Top = 9, Style = CellStyle.Strikethrough },
            Section(11, "Ausrichtung"),
            new Label("linksbündig") { Left = 0, Right = 0, Top = 12, TextAlign = TextAlignment.Left },
            new Label("zentriert") { Left = 0, Right = 0, Top = 13, TextAlign = TextAlignment.Center },
            new Label("rechtsbündig") { Left = 0, Right = 0, Top = 14, TextAlign = TextAlignment.Right });
    }

    private static Panel OutputPage(ConsoleApp app, OutputField output, Checkbox typewriter, Label status)
    {
        var append = new Button("Zeile anhängen") { Left = 0, Bottom = 0 };
        int counter = 0;
        append.Clicked += () => output.AppendLine($"Zeile {++counter} – umgebrochene Zeilen behalten ihre Einrückung.", Color.Cyan);

        var task = new Button("Task starten") { Left = 19, Bottom = 0 };
        task.Clicked += () => RunDemoTask(app, output, seconds: 3, progress: null);

        output.AppendLine("Willkommen bei ConsoleRender!", Color.Cyan);
        output.AppendLine("");
        output.AppendLine("/echo, /color und /task schreiben hierher; Bild auf/ab scrollt.", Color.Gray);
        output.AppendLine("");

        return Fill(
            Info(0, "Ein scrollbares Farb-Log. Task-Zeilen tragen einen Spinner,"),
            Info(1, "bis Complete/Fail sie mit Haken oder Kreuz einfrieren."),
            output, append, task, typewriter);
    }

    private static Panel TextBoxPage()
    {
        var plain = new TextBox { Left = 0, Top = 3, Right = 0, Height = 1, Placeholder = "ohne Rahmen" };
        var lines = new TextBox
        {
            Left = 0, Top = 6, Right = 0, Height = 3,
            BorderMode = BorderMode.TopAndBottom, BorderColor = Color.Magenta,
            Placeholder = "Linien oben und unten",
        };
        var full = new TextBox
        {
            Left = 0, Top = 10, Right = 0, Height = 3,
            BorderMode = BorderMode.Full, BorderColor = Color.Cyan,
            Placeholder = "voller Rahmen",
        };
        return Fill(
            Info(0, "Einzeilige Eingabe mit Cursor, Scrolling und Zwischenablage"),
            Info(1, "(Strg+C kopiert, Strg+V fügt ein, Strg+U leert)."),
            plain, lines, full);
    }

    private static Panel EditorPage()
    {
        var editor = new TextArea
        {
            Left = 0, Top = 3, Right = 0, Bottom = 0,
            Highlighter = new MarkdownHighlighter(),
            Text = SampleMarkdown,
        };
        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true));
        return Fill(
            Info(0, "Mehrzeiliger Editor, Markdown wird beim Tippen eingefärbt."),
            Info(1, "Enter bricht um, Strg+Pos1/Ende springt, /editor öffnet ihn modal."),
            editor);
    }

    private static Panel SearchPage(Label status)
    {
        var search = new SearchBox(
            "Berlin", "Hamburg", "München", "Köln", "Frankfurt",
            "Stuttgart", "Düsseldorf", "Dortmund", "Essen", "Leipzig")
        {
            Left = 0, Top = 3, Width = 32, Bottom = 0,
            EmptyText = "keine Treffer",
        };
        search.Input.Placeholder = "Stadt suchen…";
        search.ItemActivated += (_, item) => status.Text = $"Stadt gewählt: {item}";
        return Fill(
            Info(0, "Tippen filtert, Pfeiltasten wählen, Enter aktiviert,"),
            Info(1, "Escape leert die Suche. Die Feature-Liste links ist auch eins."),
            search);
    }

    private static Panel ChoicesPage(Label status, Frame[] frames, CommandInput input)
    {
        var menu = new SelectMenu("Übersicht", "Farben & Effekte", "Layout & Anker", "Zwischenablage", "Über")
        {
            Left = 0, Top = 3, Width = 30, Height = 5,
        };
        menu.SelectionChanged += index => status.Text = $"Menü: {menu.Items[index]}";

        var option1 = new Checkbox("Farbige Ausgabe") { Left = 0, Top = 10, Checked = true };
        var option2 = new Checkbox("Statuszeile pulsieren") { Left = 0, Top = 11 };
        option2.CheckedChanged += on => status.Effect = on ? TextEffect.Pulse : TextEffect.None;

        var borderChoice = new RadioGroup("Einfach", "Doppelt", "Rund", "Fett")
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
                frame.Border = style;
            input.Border = style;
            status.Text = $"Rahmenstil: {borderChoice.SelectedItem}";
        };

        return Fill(
            Info(0, "Menüs, Checkboxen und Radiogruppen – Leertaste schaltet um."),
            Section(2, "Menü"), menu,
            Section(9, "Optionen"), option1, option2,
            Section(13, "Rahmenstil (wirkt sofort)"), borderChoice);
    }

    private static Panel ProgressPage(ProgressBar progress, Label status)
    {
        var plus = new Button("+10 %") { Left = 0, Top = 5 };
        plus.Clicked += () => progress.Value = Math.Min(100, progress.Value + 10);
        var minus = new Button("−10 %") { Left = 10, Top = 5 };
        minus.Clicked += () => progress.Value = Math.Max(0, progress.Value - 10);

        var endless = new Checkbox("Endlosanimation") { Left = 22, Top = 5 };
        endless.CheckedChanged += on =>
        {
            progress.Indeterminate = on;
            status.Text = on ? "Fortschritt: Endlosanimation." : "Fortschritt: bestimmt.";
        };

        var spinner = new Spinner { Left = 0, Top = 8, Text = "arbeitet…", Foreground = Color.Cyan };
        var spinnerActive = new Checkbox("Spinner aktiv") { Left = 20, Top = 8, Checked = true };
        spinnerActive.CheckedChanged += on => spinner.Active = on;

        return Fill(
            Info(0, "Der Balken füllt achtelzellengenau; /progress und /task steuern ihn."),
            progress, plus, minus, endless,
            spinner, spinnerActive);
    }

    private static Panel BorderPage()
    {
        var styles = new (string Name, BorderStyle Style)[]
        {
            ("Einfach", BorderStyle.Single),
            ("Doppelt", BorderStyle.Double),
            ("Rund", BorderStyle.Rounded),
            ("Fett", BorderStyle.Thick),
            ("Ascii", BorderStyle.Ascii),
        };
        var page = new Panel { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        page.Add(Info(0, "Frames tragen Titel und einen von fünf Rahmenstilen."));
        for (int i = 0; i < styles.Length; i++)
        {
            var frame = new Frame(styles[i].Name)
            {
                Left = i % 3 * 18, Top = 2 + i / 3 * 6, Width = 16, Height = 5,
                Border = styles[i].Style,
                BorderColor = Color.Cyan,
            };
            frame.Add(new Label("Inhalt") { Left = 1, Top = 1, Foreground = Color.Gray });
            page.Add(frame);
        }
        return page;
    }

    private static Panel DialogPage(ConsoleApp app, Label status)
    {
        var info = new Button("InfoBox zeigen") { Left = 0, Top = 3 };
        info.Clicked += () => app.ShowInfo("Information",
            "Eine modale Infobox. Der Hintergrund wird abgedunkelt, alle Eingaben gehen an den Dialog.");

        var confirm = new Button("Rückfrage zeigen") { Left = 0, Top = 5 };
        confirm.Clicked += () => app.ShowConfirm("Rückfrage", "Änderungen vor dem Beenden speichern?",
            ["Speichern", "Verwerfen", "Abbrechen"],
            (_, label) => status.Text = $"Gewählt: {label}");

        var editor = new Button("Markdown-Editor öffnen") { Left = 0, Top = 7 };
        editor.Clicked += () => ShowEditorDialog(app, status);

        return Fill(
            Info(0, "Modale Dialoge legen sich über die Oberfläche; Escape schließt."),
            Info(1, "Buttons reagieren auf Enter oder Leertaste."),
            info, confirm, editor);
    }

    private static Panel AsciiPage(AsciiArt art, OutputField output, Label status)
    {
        var paste = new Button("Zwischenablage einfügen") { Left = 0, Bottom = 0 };
        paste.Clicked += () => PasteClipboard(art, output, status);

        var reset = new Button("Logo wiederherstellen") { Left = 27, Bottom = 0 };
        reset.Clicked += () =>
        {
            art.SetText(Logo);
            status.Text = "Logo wiederhergestellt.";
        };

        return Fill(
            Info(0, "ASCII-Grafik, einfarbig oder als farbiges Zeichenraster."),
            Info(1, "Ein Bild aus der Zwischenablage wird zu ASCII-Art (/paste)."),
            art, paste, reset);
    }

    private static Panel LayoutPage()
    {
        var playground = new Frame("Spielfläche")
        {
            Left = 0, Top = 4, Right = 0, Bottom = 0,
            BorderColor = Color.DarkGray,
        };
        playground.AddRange(
            new Label("Left=0, Top=0") { Left = 0, Top = 0, Foreground = Color.Cyan },
            new Label("Right=0, Top=0") { Right = 0, Top = 0, Foreground = Color.Cyan },
            new Label("Left=0, Bottom=0") { Left = 0, Bottom = 0, Foreground = Color.Cyan },
            new Label("Right=0, Bottom=0") { Right = 0, Bottom = 0, Foreground = Color.Cyan },
            new Label("zentriert (ohne Anker)")
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Foreground = Color.Yellow, Style = CellStyle.Bold,
            },
            new Label("Left=4 und Right=4 gesetzt → dehnt sich beim Resize")
            {
                Left = 4, Right = 4, Top = 2,
                TextAlign = TextAlignment.Center,
                Background = Color.DarkBlue, Foreground = Color.White,
            });
        return Fill(
            Info(0, "Anker wie bei CSS absolute: Left/Top/Right/Bottom plus Width/Height."),
            Info(1, "Beide Anker gesetzt = das Element wächst mit dem Terminal;"),
            Info(2, "ohne Anker entscheidet die Ausrichtung. Fenstergröße ändern!"),
            playground);
    }

    // --- shared behaviour ----------------------------------------------------------------

    /// <summary>Simulates work: a task line counts up and optionally drives the progress bar.</summary>
    private static void RunDemoTask(ConsoleApp app, OutputField output, double seconds, ProgressBar? progress)
    {
        var task = output.BeginTask("Verarbeite Daten…");
        double done = 0;

        void OnTick(TimeSpan delta)
        {
            done += delta.TotalSeconds;
            double percent = Math.Min(100, done / seconds * 100);
            if (progress is not null)
            {
                progress.Indeterminate = false;
                progress.Value = percent;
            }
            task.Text = $"Verarbeite Daten… {percent:0} %";
            if (done >= seconds)
            {
                task.Complete($"Daten verarbeitet ({seconds:0.#} s).");
                app.Tick -= OnTick;
            }
        }

        app.Tick += OnTick;
    }

    private static void PasteClipboard(AsciiArt art, OutputField output, Label status)
    {
        if (Clipboard.TryGetImage(out var image))
        {
            var ascii = AsciiImageConverter.Convert(image, Math.Max(10, art.Bounds.Width));
            art.SetImage(ascii);
            status.Text = $"Bild übernommen ({image.Width}×{image.Height} Pixel).";
            return;
        }

        if (Clipboard.TryGetText(out string clipboardText))
        {
            foreach (string line in clipboardText.Replace("\r", "").Split('\n'))
                output.AppendLine(line, Color.Cyan);
            status.Text = "Text aus der Zwischenablage ins Ausgabe-Log übernommen.";
            return;
        }

        status.Text = "Die Zwischenablage enthält weder Text noch ein Bild.";
    }

    private static void ShowEditorDialog(ConsoleApp app, Label status)
    {
        var dialog = new MarkdownEditorDialog(SampleMarkdown);
        dialog.CloseRequested += () => status.Text = "Editor geschlossen.";
        app.ShowDialog(dialog);
    }

    /// <summary>
    /// A modal Markdown editor — shows how consumers build custom dialogs: derive from
    /// ModalControl, add focusable children, call Close() when done. The dialog itself stays
    /// out of the focus cycle so the editor receives the keys; Escape falls through to OnKey.
    /// </summary>
    private sealed class MarkdownEditorDialog : ModalControl
    {
        private readonly TextArea editor;

        public MarkdownEditorDialog(string initialText)
        {
            Focusable = false;
            editor = new TextArea
            {
                Left = 2, Top = 1, Right = 2, Bottom = 1,
                Highlighter = new MarkdownHighlighter(),
                Text = initialText,
            };
            // The Text setter leaves the cursor at the end; start reading at the top.
            editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true));
            Add(editor);
        }

        public string Text => editor.Text;

        protected override Size GetPreferredSize(Size available) => new(
            Math.Clamp(available.Width - 20, 44, 76),
            Math.Clamp(available.Height - 6, 10, 22));

        public override bool OnKey(ConsoleKeyInfo key)
        {
            if (key.Key != ConsoleKey.Escape) return false;
            Close();
            return true;
        }

        protected override void Draw(ConsoleBuffer buffer)
        {
            buffer.FillRect(Bounds, ' ', Color.White, Color.DarkBlue);
            buffer.DrawBorder(Bounds, BorderStyle.Rounded, Color.Cyan, Color.DarkBlue,
                "Markdown-Editor · Esc schließt", Color.Yellow);
        }
    }

    // --- commands and key bindings -------------------------------------------------------

    private static void WireCommands(ConsoleApp app, Ui ui)
    {
        var commands = ui.Input.Commands;

        ui.Input.Submitted += text => ui.Output.AppendLine($"> {text}", Color.White);

        ui.Input.CommandExecuted += result =>
        {
            if (!result.Success)
                ui.Output.AppendLine(result.Message, Color.Red);
        };

        commands.Register("help", "Zeigt Befehle und Tastenkürzel", _ =>
        {
            FillHelpPage(app, ui);
            ui.ShowFeature("Befehle & Tastenkürzel");
        });

        commands.Register("feature", "Wechselt das Feature: /feature <name>", args =>
        {
            string query = string.Join(' ', args);
            Guard.Against.NullOrWhiteSpace(query, nameof(args));
            string? match = ui.FeatureNames
                .FirstOrDefault(n => n.Contains(query, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                ui.Status.Text = $"Kein Feature passt zu \"{query}\".";
                return;
            }
            ui.ShowFeature(match);
        });

        commands.Register("echo", "Gibt den Text im Ausgabe-Log aus", args =>
        {
            ui.Output.AppendLine(string.Join(' ', args), Color.Green);
            ui.ShowFeature("Ausgabe-Log & Task-Zeilen");
        });

        commands.Register("clear", "Leert das Ausgabe-Log", _ =>
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
            ui.ShowFeature("Ausgabe-Log & Task-Zeilen");
        });

        commands.Register("typewriter", "Schaltet den Schreibmaschineneffekt: /typewriter <an|aus>", args =>
        {
            bool on = args.Length == 0 ? !ui.Output.Typewriter : args[0] is "an" or "on" or "1";
            ui.TypewriterOption.Checked = on;
            ui.Output.Typewriter = on;
            ui.Status.Text = on ? "Typewriter-Effekt an." : "Typewriter-Effekt aus.";
        });

        commands.Register("task", "Simuliert eine Aufgabe mit Task-Zeile: /task [sekunden]", args =>
        {
            double seconds = args.Length > 0 ? double.Parse(args[0]) : 3;
            Guard.Against.NegativeOrZero(seconds, nameof(args));
            RunDemoTask(app, ui.Output, seconds, ui.Progress);
            ui.ShowFeature("Ausgabe-Log & Task-Zeilen");
        });

        commands.Register("progress", "Setzt den Fortschrittsbalken: /progress <0-100|endlos>", args =>
        {
            if (args.Length == 0 || args[0] is "endlos" or "marquee")
            {
                ui.Progress.Indeterminate = !ui.Progress.Indeterminate;
                ui.Status.Text = ui.Progress.Indeterminate
                    ? "Fortschritt: Endlosanimation."
                    : "Fortschritt: bestimmt.";
            }
            else
            {
                ui.Progress.Indeterminate = false;
                ui.Progress.Value = double.Parse(args[0]);
                ui.Status.Text = $"Fortschritt: {ui.Progress.Value:0} %";
            }
            ui.ShowFeature("Fortschritt & Spinner");
        });

        commands.Register("paste", "Fügt Text oder ein Bild aus der Zwischenablage ein", _ =>
        {
            PasteClipboard(ui.Art, ui.Output, ui.Status);
            ui.ShowFeature("ASCII-Grafik & Zwischenablage");
        });

        commands.Register("logo", "Stellt die ASCII-Grafik auf das Logo zurück", _ =>
        {
            ui.Art.SetText(Logo);
            ui.Status.Text = "Logo wiederhergestellt.";
            ui.ShowFeature("ASCII-Grafik & Zwischenablage");
        });

        commands.Register("copy", "Kopiert Text in die Zwischenablage: /copy <Text>", args =>
        {
            string text = string.Join(' ', args);
            Guard.Against.NullOrWhiteSpace(text, nameof(args));
            ui.Status.Text = Clipboard.TrySetText(text)
                ? "In die Zwischenablage kopiert."
                : "Zugriff auf die Zwischenablage fehlgeschlagen.";
        });

        commands.Register("busy", "Schaltet den Spinner in der Kopfzeile um", _ =>
        {
            ui.Spinner.Active = !ui.Spinner.Active;
            ui.Spinner.Text = ui.Spinner.Active ? "arbeitet…" : "fertig";
        });

        commands.Register("border", "Rahmen des Eingabefelds: /border <voll|linien|keiner>", args =>
        {
            ui.Input.BorderMode = args.Length == 0
                ? (BorderMode)(((int)ui.Input.BorderMode + 1) % 3)
                : args[0].ToLowerInvariant() switch
                {
                    "voll" or "full" => BorderMode.Full,
                    "linien" or "lines" => BorderMode.TopAndBottom,
                    "keiner" or "kein" or "none" => BorderMode.None,
                    _ => throw new ArgumentException($"Unbekannter Rahmen: {args[0]}"),
                };
            ui.Status.Text = $"Eingabefeld-Rahmen: {ui.Input.BorderMode}";
        });

        commands.Register("info", "Zeigt eine modale Infobox: /info <Text>", args =>
            app.ShowInfo("Information", args.Length > 0
                ? string.Join(' ', args)
                : "Eine modale Infobox. Der Hintergrund wird abgedunkelt, alle Eingaben gehen an den Dialog."));

        commands.Register("confirm", "Zeigt einen Rückfragedialog", _ =>
            app.ShowConfirm("Rückfrage", "Änderungen vor dem Beenden speichern?",
                ["Speichern", "Verwerfen", "Abbrechen"],
                (_, label) => ui.Status.Text = $"Gewählt: {label}"));

        commands.Register("editor", "Öffnet den Markdown-Editor", _ => ShowEditorDialog(app, ui.Status));

        commands.Register("exit", "Beendet die Demo", _ => ConfirmExit(app, ui));
    }

    /// <summary>Asks before quitting — the case a button row handles better than a shortcut.</summary>
    private static void ConfirmExit(ConsoleApp app, Ui ui)
    {
        app.ShowConfirm("Beenden", "Demo wirklich beenden?", ["Beenden", "Zurück"], (index, _) =>
        {
            if (index == 0)
                app.Exit();
            else
                ui.Status.Text = "Beenden abgebrochen.";
        });
    }

    private static void FillHelpPage(ConsoleApp app, Ui ui)
    {
        ui.HelpOutput.Clear();
        ui.HelpOutput.AppendLine("Befehle beginnen mit / – Tab vervollständigt sie.", Color.Gray);
        ui.HelpOutput.AppendLine("");
        ui.HelpOutput.AppendLine("Befehle:", Color.Yellow);
        foreach (var command in ui.Input.Commands.All)
            ui.HelpOutput.AppendLine($"  /{command.Name,-12} {command.Description}", Color.Gray);
        ui.HelpOutput.AppendLine("");
        ui.HelpOutput.AppendLine("Tastenkürzel:", Color.Yellow);
        foreach (var binding in app.KeyBindings.All)
            ui.HelpOutput.AppendLine($"  {binding.Combo,-12} {binding.Description}", Color.Gray);
        ui.HelpOutput.AppendLine("  Tab/Shift+Tab  Fokus wechseln · Pfeile bewegen · Leertaste schaltet", Color.Gray);
    }

    private static void WireKeyBindings(ConsoleApp app, Ui ui)
    {
        app.KeyBindings.Register(ConsoleKey.F1, "Hilfe anzeigen", () =>
        {
            FillHelpPage(app, ui);
            ui.ShowFeature("Befehle & Tastenkürzel");
        });

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Beenden", () => ConfirmExit(app, ui));

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.L), "Ausgabe leeren", () =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Ausgabe geleert.";
        });
    }
}
