namespace ConsoleRender.Demo;

/// <summary>
/// A modal Markdown editor — shows how consumers build custom dialogs: derive from
/// ModalControl, add focusable children, call Close() when done. The dialog itself stays
/// out of the focus cycle so the editor receives the keys; Escape falls through to OnKey.
/// </summary>
internal sealed class MarkdownEditorDialog : ModalControl
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
