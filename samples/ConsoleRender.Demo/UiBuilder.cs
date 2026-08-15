using ConsoleRender.Demo.Pages;

namespace ConsoleRender.Demo;

/// <summary>
/// Builds the demo's layout: header, feature navigation on the left, the swappable feature
/// panel on the right, and the command input at the bottom. Pages are built once so their
/// state survives switching back and forth.
/// </summary>
internal static class UiBuilder
{
    public static Ui Build(ConsoleApp app)
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
        var art = new AsciiArt(DemoContent.Logo)
        {
            Foreground = Color.Green,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Middle,
        };

        var progress = new ProgressBar { Left = 0, Top = 3, Right = 0, Height = 1 };
        var helpOutput = new OutputField { Left = 0, Top = 0, Right = 0, Bottom = 0 };

        var tabsPage = TabsPage.Build(out var tabsControl);
        var pages = new List<(string Name, Control Page)>
        {
            ("Overview", OverviewPage.Build()),
            ("Labels & Effects", LabelPage.Build()),
            ("Output Log & Task Lines", OutputPage.Build(app, output, typewriterOption, status)),
            ("Text Fields", TextBoxPage.Build()),
            ("Markdown Editor", EditorPage.Build()),
            ("Search Box", SearchPage.Build(status)),
            ("Choices & Options", ChoicesPage.Build(status, [leftFrame, rightFrame], input, tabsControl)),
            ("Multi-Select", MultiSelectPage.Build(status)),
            ("Tabs", tabsPage),
            ("Table", TablePage.Build(status)),
            ("Tree View", TreeViewPage.Build(status)),
            ("Progress & Spinner", ProgressPage.Build(progress, status)),
            ("Frames & Styles", BorderPage.Build()),
            ("Dialogs & Buttons", DialogPage.Build(app, status)),
            ("ASCII Art & Clipboard", AsciiPage.Build(art, output, status)),
            ("Layout & Anchors", LayoutPage.Build()),
            ("Commands & Shortcuts", PageHelpers.Fill(helpOutput)),
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
}
