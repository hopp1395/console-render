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
        # Markdown Editor

        The editor highlights **bold**, *italic*, `code` and ~~strikethrough~~,
        plus ***both at once***.

        - List item with [a link](https://example.org)
        1. Numbered list

        > Quotes appear in italic gray.

        ```csharp
        var app = new ConsoleApp();
        app.Run();
        ```

        ---
        Esc closes the editor.
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
        var title = new Label("╔═ ConsoleRender ═ TUI Framework for .NET ═╗")
        {
            Left = 0, Right = 0, Top = 0, Height = 1,
            TextAlign = TextAlignment.Center,
            Effect = TextEffect.Rainbow,
            Style = CellStyle.Bold,
        };

        var spinner = new Spinner { Text = "ready", Top = 1, Left = 2, Foreground = Color.Green };

        var hint = new Label("F1 Help · Tab Focus · Ctrl+Q Quit")
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
            Placeholder = "› Type text or /command – Tab completes",
        };

        var status = new Label("Ready.") { Left = 1, Bottom = 0, Foreground = Color.DarkGray };

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

        var tabsPage = TabsPage(out var tabsControl);
        var pages = new List<(string Name, Control Page)>
        {
            ("Overview", OverviewPage()),
            ("Labels & Effects", LabelPage()),
            ("Output Log & Task Lines", OutputPage(app, output, typewriterOption, status)),
            ("Text Fields", TextBoxPage()),
            ("Markdown Editor", EditorPage()),
            ("Search Box", SearchPage(status)),
            ("Choices & Options", ChoicesPage(status, [leftFrame, rightFrame], input, tabsControl)),
            ("Tabs", tabsPage),
            ("Table", TablePage(status)),
            ("Progress & Spinner", ProgressPage(progress, status)),
            ("Frames & Styles", BorderPage()),
            ("Dialogs & Buttons", DialogPage(app, status)),
            ("ASCII Art & Clipboard", AsciiPage(art, output, status)),
            ("Layout & Anchors", LayoutPage()),
            ("Commands & Shortcuts", Fill(helpOutput)),
        };
        var pageByName = pages.ToDictionary(p => p.Name, p => p.Page);
        var featureNames = pages.Select(p => p.Name).ToList();

        var nav = new SearchBox(featureNames.ToArray())
        {
            Left = 0, Top = 0, Right = 0, Bottom = 0,
            EmptyText = "no matches",
        };
        nav.Input.Placeholder = "Search features…";
        leftFrame.Add(nav);

        Control? currentPage = null;
        void ShowFeature(string name)
        {
            if (!pageByName.TryGetValue(name, out var page) || ReferenceEquals(page, currentPage))
            {
                return;
            }

            if (currentPage is not null)
            {
                rightFrame.Remove(currentPage);
            }

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
            var showNav = width >= 64;
            leftFrame.Visible = showNav;
            rightFrame.Left = showNav ? 30 : 0;
            hint.Visible = width >= 70;

            var inputRows = input.BorderMode == BorderMode.None ? 1 : 3;
            input.Height = inputRows;
            leftFrame.Bottom = rightFrame.Bottom = inputRows + 1;
        }

        app.Tick += _ => ApplyResponsiveLayout(app.Width);

        typewriterOption.CheckedChanged += on =>
        {
            output.Typewriter = on;
            status.Text = on ? "Typewriter effect on." : "Typewriter effect off.";
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

    private static Label Info(int top, string text, Color? color = null)
    {
        return new(text) { Left = 0, Top = top, Foreground = color ?? Color.Gray };
    }

    private static Label Section(int top, string text)
    {
        return new(text) { Left = 0, Top = top, Foreground = Color.Yellow, Style = CellStyle.Bold };
    }

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
            new Label("A double-buffered TUI framework for .NET.")
            {
                Left = 0, Right = 0, Top = 10, TextAlign = TextAlignment.Center, Foreground = Color.White,
            },
            new Label("Pick a feature on the left – arrow keys move, typing filters.")
            {
                Left = 0, Right = 0, Top = 12, TextAlign = TextAlignment.Center, Foreground = Color.Gray,
            },
            new Label("Tab switches focus, F1 shows help, /help lists all commands.")
            {
                Left = 0, Right = 0, Top = 13, TextAlign = TextAlignment.Center, Foreground = Color.Gray,
            });
    }

    private static Panel LabelPage()
    {
        return Fill(
            Info(0, "Labels show text with color, style and animation."),
            Section(2, "Effects"),
            new Label("No effect – but with color") { Left = 2, Top = 3, Foreground = Color.Cyan },
            new Label("Blink: on and off") { Left = 2, Top = 4, Effect = TextEffect.Blink, Foreground = Color.Yellow },
            new Label("Rainbow: a drifting color gradient") { Left = 2, Top = 5, Effect = TextEffect.Rainbow },
            new Label("Pulse: brightness breathes") { Left = 2, Top = 6, Effect = TextEffect.Pulse, Foreground = Color.Magenta },
            Section(8, "Styles"),
            new Label("bold") { Left = 2, Top = 9, Style = CellStyle.Bold },
            new Label("italic") { Left = 8, Top = 9, Style = CellStyle.Italic },
            new Label("underlined") { Left = 16, Top = 9, Style = CellStyle.Underline },
            new Label("strikethrough") { Left = 31, Top = 9, Style = CellStyle.Strikethrough },
            Section(11, "Alignment"),
            new Label("left-aligned") { Left = 0, Right = 0, Top = 12, TextAlign = TextAlignment.Left },
            new Label("centered") { Left = 0, Right = 0, Top = 13, TextAlign = TextAlignment.Center },
            new Label("right-aligned") { Left = 0, Right = 0, Top = 14, TextAlign = TextAlignment.Right });
    }

    private static Panel OutputPage(ConsoleApp app, OutputField output, Checkbox typewriter, Label status)
    {
        var append = new Button("Append line") { Left = 0, Bottom = 0 };
        var counter = 0;
        append.Clicked += () => output.AppendLine($"Line {++counter} – wrapped lines keep their indentation.", Color.Cyan);

        var task = new Button("Start task") { Left = 19, Bottom = 0 };
        task.Clicked += () => RunDemoTask(app, output, seconds: 3, progress: null);

        output.AppendLine("Welcome to ConsoleRender!", Color.Cyan);
        output.AppendLine("");
        output.AppendLine("/echo, /color and /task write here; Page Up/Down scrolls.", Color.Gray);
        output.AppendLine("");

        return Fill(
            Info(0, "A scrollable color log. Task lines carry a spinner,"),
            Info(1, "until Complete/Fail freezes them with a check or a cross."),
            output, append, task, typewriter);
    }

    private static Panel TextBoxPage()
    {
        var plain = new TextBox { Left = 0, Top = 3, Right = 0, Height = 1, Placeholder = "no border" };
        var lines = new TextBox
        {
            Left = 0, Top = 6, Right = 0, Height = 3,
            BorderMode = BorderMode.TopAndBottom, BorderColor = Color.Magenta,
            Placeholder = "lines above and below",
        };
        var full = new TextBox
        {
            Left = 0, Top = 10, Right = 0, Height = 3,
            BorderMode = BorderMode.Full, BorderColor = Color.Cyan,
            Placeholder = "full border",
        };
        return Fill(
            Info(0, "Single-line input with cursor, scrolling and clipboard support"),
            Info(1, "(Ctrl+C copies, Ctrl+V pastes, Ctrl+U clears)."),
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
            Info(0, "Multi-line editor, Markdown is highlighted as you type."),
            Info(1, "Enter wraps, Ctrl+Home/End jumps, /editor opens it modally."),
            editor);
    }

    private static Panel SearchPage(Label status)
    {
        var search = new SearchBox(
            "Berlin", "Hamburg", "Munich", "Cologne", "Frankfurt",
            "Stuttgart", "Düsseldorf", "Dortmund", "Essen", "Leipzig")
        {
            Left = 0, Top = 3, Width = 32, Bottom = 0,
            EmptyText = "no matches",
        };
        search.Input.Placeholder = "Search city…";
        search.ItemActivated += (_, item) => status.Text = $"City selected: {item}";
        return Fill(
            Info(0, "Typing filters, arrow keys select, Enter activates,"),
            Info(1, "Escape clears the search. The feature list on the left is one too."),
            search);
    }

    private static Panel ChoicesPage(Label status, Frame[] frames, CommandInput input, TabControl tabs)
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

    private static Panel TabsPage(out TabControl tabs)
    {
        tabs = new TabControl { Left = 0, Top = 3, Right = 0, Bottom = 0 };

        tabs.AddTab("Overview", Fill(
            new Label("This tab shows only text – no focusable content behind it.")
            {
                Left = 0, Top = 0, Foreground = Color.Gray,
            }));

        var name = new TextBox { Left = 0, Top = 0, Right = 0, Placeholder = "Enter name…" };
        var subscribe = new Checkbox("Subscribe to newsletter") { Left = 0, Top = 2 };
        tabs.AddTab("Form", Fill(name, subscribe));

        tabs.AddTab("Warning", Fill(
            new Label("A tab can override its accent color individually (red here).")
            {
                Left = 0, Top = 0, Foreground = Color.Gray,
            }), accentColor: Color.Red);

        return Fill(
            Info(0, "Arrow keys or Home/End switch the active tab."),
            Info(1, "Tab jumps from the tab header into its content, Shift+Tab back."),
            tabs);
    }

    private static Panel TablePage(Label status)
    {
        var table = new Table { Left = 0, Top = 3, Right = 0, Bottom = 0 };
        table.AddColumn("City", 16);
        table.AddColumn("Population", 12, TextAlignment.Right);
        table.AddColumn("State", 16);
        table.AddRow("Berlin", "3,700,000", "Berlin");
        table.AddRow("Hamburg", "1,900,000", "Hamburg");
        table.AddRow("Munich", "1,500,000", "Bavaria");
        table.AddRow("Cologne", "1,100,000", "North Rhine-Westphalia");
        table.AddRow("Frankfurt", "770,000", "Hesse");
        table.AddRow("Stuttgart", "630,000", "Baden-Württemberg");
        table.AddRow("Düsseldorf", "620,000", "North Rhine-Westphalia");
        table.AddRow("Leipzig", "600,000", "Saxony");
        table.SelectionChanged += i => status.Text = $"Row selected: {table.Rows[i][0]}";
        table.RowActivated += i => status.Text = $"Row activated: {table.Rows[i][0]}";

        return Fill(
            Info(0, "Arrow keys or Home/End move the selection, Enter activates the row."),
            Info(1, "Cells too long to fit scroll automatically, but only in the selected row."),
            table);
    }

    private static Panel ProgressPage(ProgressBar progress, Label status)
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

    private static Panel BorderPage()
    {
        var styles = new (string Name, BorderStyle Style)[]
        {
            ("Simple", BorderStyle.Single),
            ("Double", BorderStyle.Double),
            ("Rounded", BorderStyle.Rounded),
            ("Thick", BorderStyle.Thick),
            ("Ascii", BorderStyle.Ascii),
        };
        var page = new Panel { Left = 0, Top = 0, Right = 0, Bottom = 0 };
        page.Add(Info(0, "Frames carry a title and one of five border styles."));
        for (var i = 0; i < styles.Length; i++)
        {
            var frame = new Frame(styles[i].Name)
            {
                Left = i % 3 * 18, Top = 2 + i / 3 * 6, Width = 16, Height = 5,
                Border = styles[i].Style,
                BorderColor = Color.Cyan,
            };
            frame.Add(new Label("Content") { Left = 1, Top = 1, Foreground = Color.Gray });
            page.Add(frame);
        }
        return page;
    }

    private static Panel DialogPage(ConsoleApp app, Label status)
    {
        var info = new Button("Show info box") { Left = 0, Top = 3 };
        info.Clicked += () => app.ShowInfo("Information",
            "A modal info box. The background is dimmed, all input goes to the dialog.");

        var confirm = new Button("Show confirmation") { Left = 0, Top = 5 };
        confirm.Clicked += () => app.ShowConfirm("Confirm", "Save changes before quitting?",
            ["Save", "Discard", "Cancel"],
            (_, label) => status.Text = $"Chosen: {label}");

        var editor = new Button("Open Markdown editor") { Left = 0, Top = 7 };
        editor.Clicked += () => ShowEditorDialog(app, status);

        return Fill(
            Info(0, "Modal dialogs lie on top of the UI; Escape closes them."),
            Info(1, "Buttons respond to Enter or Space."),
            info, confirm, editor);
    }

    private static Panel AsciiPage(AsciiArt art, OutputField output, Label status)
    {
        var paste = new Button("Paste clipboard") { Left = 0, Bottom = 0 };
        paste.Clicked += () => PasteClipboard(art, output, status);

        var reset = new Button("Restore logo") { Left = 27, Bottom = 0 };
        reset.Clicked += () =>
        {
            art.SetText(Logo);
            status.Text = "Logo restored.";
        };

        return Fill(
            Info(0, "ASCII art, single-color or as a colored character grid."),
            Info(1, "An image from the clipboard becomes ASCII art (/paste)."),
            art, paste, reset);
    }

    private static Panel LayoutPage()
    {
        var playground = new Frame("Playground")
        {
            Left = 0, Top = 4, Right = 0, Bottom = 0,
            BorderColor = Color.DarkGray,
        };
        playground.AddRange(
            new Label("Left=0, Top=0") { Left = 0, Top = 0, Foreground = Color.Cyan },
            new Label("Right=0, Top=0") { Right = 0, Top = 0, Foreground = Color.Cyan },
            new Label("Left=0, Bottom=0") { Left = 0, Bottom = 0, Foreground = Color.Cyan },
            new Label("Right=0, Bottom=0") { Right = 0, Bottom = 0, Foreground = Color.Cyan },
            new Label("centered (no anchors)")
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                Foreground = Color.Yellow, Style = CellStyle.Bold,
            },
            new Label("Left=4 and Right=4 set → stretches on resize")
            {
                Left = 4, Right = 4, Top = 2,
                TextAlign = TextAlignment.Center,
                Background = Color.DarkBlue, Foreground = Color.White,
            });
        return Fill(
            Info(0, "Anchors like CSS absolute: Left/Top/Right/Bottom plus Width/Height."),
            Info(1, "Both anchors set = the element grows with the terminal;"),
            Info(2, "without anchors, alignment decides. Resize the window!"),
            playground);
    }

    // --- shared behaviour ----------------------------------------------------------------

    /// <summary>Simulates work: a task line counts up and optionally drives the progress bar.</summary>
    private static void RunDemoTask(ConsoleApp app, OutputField output, double seconds, ProgressBar? progress)
    {
        var task = output.BeginTask("Processing data…");
        double done = 0;

        void OnTick(TimeSpan delta)
        {
            done += delta.TotalSeconds;
            var percent = Math.Min(100, done / seconds * 100);
            if (progress is not null)
            {
                progress.Indeterminate = false;
                progress.Value = percent;
            }
            task.Text = $"Processing data… {percent:0} %";
            if (done >= seconds)
            {
                task.Complete($"Data processed ({seconds:0.#} s).");
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
            status.Text = $"Image imported ({image.Width}×{image.Height} pixels).";
            return;
        }

        if (Clipboard.TryGetText(out var clipboardText))
        {
            foreach (var line in clipboardText.Replace("\r", "").Split('\n'))
            {
                output.AppendLine(line, Color.Cyan);
            }

            status.Text = "Text from the clipboard added to the output log.";
            return;
        }

        status.Text = "The clipboard contains neither text nor an image.";
    }

    private static void ShowEditorDialog(ConsoleApp app, Label status)
    {
        var dialog = new MarkdownEditorDialog(SampleMarkdown);
        dialog.CloseRequested += () => status.Text = "Editor closed.";
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

        protected override Size GetPreferredSize(Size available)
        {
            return new(
                Math.Clamp(available.Width - 20, 44, 76),
                Math.Clamp(available.Height - 6, 10, 22));
        }

        public override bool OnKey(ConsoleKeyInfo key)
        {
            if (key.Key != ConsoleKey.Escape)
            {
                return false;
            }

            Close();
            return true;
        }

        protected override void Draw(ConsoleBuffer buffer)
        {
            buffer.FillRect(Bounds, ' ', Color.White, Color.DarkBlue);
            buffer.DrawBorder(Bounds, BorderStyle.Rounded, Color.Cyan, Color.DarkBlue,
                "Markdown Editor · Esc closes", Color.Yellow);
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
            RunDemoTask(app, ui.Output, seconds, ui.Progress);
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
            PasteClipboard(ui.Art, ui.Output, ui.Status);
            ui.ShowFeature("ASCII Art & Clipboard");
        });

        commands.Register("logo", "Resets the ASCII art back to the logo", _ =>
        {
            ui.Art.SetText(Logo);
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

        commands.Register("editor", "Opens the Markdown editor", _ => ShowEditorDialog(app, ui.Status));

        commands.Register("exit", "Exits the demo", _ => ConfirmExit(app, ui));
    }

    /// <summary>Asks before quitting — the case a button row handles better than a shortcut.</summary>
    private static void ConfirmExit(ConsoleApp app, Ui ui)
    {
        app.ShowConfirm("Quit", "Really quit the demo?", ["Quit", "Cancel"], (index, _) =>
        {
            if (index == 0)
            {
                app.Exit();
            }
            else
            {
                ui.Status.Text = "Quit cancelled.";
            }
        });
    }

    private static void FillHelpPage(ConsoleApp app, Ui ui)
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

    private static void WireKeyBindings(ConsoleApp app, Ui ui)
    {
        app.KeyBindings.Register(ConsoleKey.F1, "Show help", () =>
        {
            FillHelpPage(app, ui);
            ui.ShowFeature("Commands & Shortcuts");
        });

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Quit", () => ConfirmExit(app, ui));

        app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.L), "Clear output", () =>
        {
            ui.Output.Clear();
            ui.Status.Text = "Output cleared.";
        });
    }
}
