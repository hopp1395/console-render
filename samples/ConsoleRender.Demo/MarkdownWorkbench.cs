namespace ConsoleRender.Demo;

/// <summary>
/// A Markdown editor paired with its rendered preview: editor only, preview only, or both
/// side by side. The preview follows every edit through TextChanged. Used by the
/// "Markdown Editor" page and the modal editor dialog.
/// </summary>
internal sealed class MarkdownWorkbench : Panel
{
    private readonly ConsoleApp app;
    private WorkbenchMode mode;

    public MarkdownWorkbench(ConsoleApp app, string text)
    {
        this.app = Guard.Against.Null(app);
        Guard.Against.Null(text);

        Editor = new TextArea
        {
            Top = 0, Bottom = 0,
            Highlighter = new MarkdownHighlighter(),
            Text = text,
        };

        // The Text setter leaves the cursor at the end; start reading at the top.
        Editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true));

        Preview = new MarkdownView { Top = 0, Bottom = 0, Markdown = text };
        Editor.TextChanged += markdown => Preview.Markdown = markdown;
        AddRange(Editor, Preview);
        Mode = WorkbenchMode.Edit;
    }

    public TextArea Editor { get; }
    public MarkdownView Preview { get; }

    public WorkbenchMode Mode
    {
        get => mode;
        private set
        {
            mode = value;
            Editor.Visible = value != WorkbenchMode.Preview;
            Preview.Visible = value != WorkbenchMode.Edit;
        }
    }

    /// <summary>Edit → Preview → Split → Edit.</summary>
    public void CycleMode()
    {
        ShowMode((WorkbenchMode)(((int)Mode + 1) % 3));
    }

    /// <summary>Switches the mode and moves focus to a visible pane.</summary>
    public void ShowMode(WorkbenchMode next)
    {
        Mode = next;
        app.SetFocus(next == WorkbenchMode.Preview ? Preview : Editor);
    }

    protected override void ArrangeChildren()
    {
        var half = ContentRect.Width / 2;
        if (Mode == WorkbenchMode.Split)
        {
            Editor.Left = 0;
            Editor.Right = null;
            Editor.Width = half;
            Preview.Left = half + 1;
        }
        else
        {
            Editor.Left = 0;
            Editor.Right = 0;
            Editor.Width = null;
            Preview.Left = 0;
        }

        Preview.Right = 0;
    }
}
