namespace ConsoleRender.Demo;

/// <summary>
/// A modal Markdown editor — shows how consumers build custom dialogs: derive from
/// ModalControl, add focusable children, call Close() when done. The dialog itself stays
/// out of the focus cycle so the editor receives the keys; F2 and Escape fall through to OnKey.
/// </summary>
internal sealed class MarkdownEditorDialog : ModalControl
{
    private readonly MarkdownWorkbench workbench;

    public MarkdownEditorDialog(ConsoleApp app, string initialText)
    {
        Focusable = false;
        workbench = new MarkdownWorkbench(app, initialText)
        {
            Left = 2, Top = 1, Right = 2, Bottom = 1,
        };

        Add(workbench);
    }

    public string Text => workbench.Editor.Text;

    protected override Size GetPreferredSize(Size available)
    {
        return new(
            Math.Clamp(available.Width - 20, 44, 110),
            Math.Clamp(available.Height - 6, 10, 26));
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {

            case ConsoleKey.F2:
                workbench.CycleMode();
                return true;

            case ConsoleKey.Escape:
                Close();
                return true;

        }

        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        buffer.FillRect(Bounds, ' ', Color.White, Color.DarkBlue);
        buffer.DrawBorder(Bounds, BorderStyle.Rounded, Color.Cyan, Color.DarkBlue,
            $"Markdown {workbench.Mode} · F2 switches · Esc closes", Color.Yellow);
    }
}
