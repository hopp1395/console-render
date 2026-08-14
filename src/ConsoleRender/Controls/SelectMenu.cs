namespace ConsoleRender;

/// <summary>
/// A scrollable selection list. Arrow keys move the highlight, Enter activates the item.
/// </summary>
public class SelectMenu : Control
{
    private int scroll;

    public List<string> Items { get; } = new();
    public int SelectedIndex { get; set; }
    public Color Foreground { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Magenta;

    /// <summary>Raised when Enter is pressed on an item.</summary>
    public event Action<int, string>? ItemActivated;

    /// <summary>Raised when the highlight moves.</summary>
    public event Action<int>? SelectionChanged;

    public SelectMenu()
    {
        Focusable = true;
    }

    public SelectMenu(params string[] items)
        : this()
    {
        Guard.Against.Null(items);
        Items.AddRange(items);
    }

    protected override Size GetPreferredSize(Size available)
    {
        return new(Items.Count == 0 ? 8 : Items.Max(i => i.Length) + 2, Math.Max(1, Items.Count));
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (Items.Count == 0) return false;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                Move(-1);
                return true;
            case ConsoleKey.DownArrow:
                Move(1);
                return true;
            case ConsoleKey.Home:
                MoveTo(0);
                return true;
            case ConsoleKey.End:
                MoveTo(Items.Count - 1);
                return true;
            case ConsoleKey.Enter:
                ItemActivated?.Invoke(SelectedIndex, Items[SelectedIndex]);
                return true;
        }
        return false;
    }

    private void Move(int delta)
    {
        MoveTo((SelectedIndex + delta + Items.Count) % Items.Count);
    }

    private void MoveTo(int index)
    {
        Guard.Against.OutOfRange(index, nameof(index), 0, Items.Count - 1);

        if (index == SelectedIndex) return;
        SelectedIndex = index;
        SelectionChanged?.Invoke(SelectedIndex);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1) return;

        // Keep selection in view.
        if (SelectedIndex < scroll) scroll = SelectedIndex;
        if (SelectedIndex >= scroll + Bounds.Height) scroll = SelectedIndex - Bounds.Height + 1;

        for (int row = 0; row < Bounds.Height; row++)
        {
            int i = scroll + row;
            if (i >= Items.Count) break;
            bool selected = i == SelectedIndex;
            var fg = selected ? AccentColor : Foreground;
            var style = selected && Focused ? CellStyle.Reverse | CellStyle.Bold
                : selected ? CellStyle.Bold
                : CellStyle.None;
            string marker = selected ? "› " : "  ";
            string text = marker + Items[i];
            if (text.Length > Bounds.Width) text = text[..Bounds.Width];
            buffer.Write(Bounds.X, Bounds.Y + row, text.PadRight(Bounds.Width), fg, default, style);
        }
    }
}
