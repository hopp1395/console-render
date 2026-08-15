namespace ConsoleRender;

/// <summary>
/// A scrollable list where any number of items can be checked. Arrow keys move the cursor,
/// Space toggles the highlighted item, Enter raises <see cref="Submitted"/> with the checked
/// items — moving the cursor alone changes nothing, the same as <see cref="RadioGroup"/>.
/// </summary>
public class MultiSelectMenu : Control
{
    private int cursor;
    private int scroll;

    public List<string> Items { get; } = new();

    public HashSet<int> CheckedIndices { get; } = new();

    public Color Foreground { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Cyan;

    /// <summary>Raised when an item's checked state changes.</summary>
    public event Action<int, bool>? ItemCheckedChanged;

    /// <summary>Raised on Enter, with the indices of the currently checked items.</summary>
    public event Action<IReadOnlyCollection<int>>? Submitted;

    public MultiSelectMenu()
    {
        Focusable = true;
    }

    public MultiSelectMenu(params string[] items)
        : this()
    {
        Guard.Against.Null(items);
        Items.AddRange(items);
    }

    protected override Size GetPreferredSize(Size available)
    {
        return new(Items.Count == 0 ? 8 : Items.Max(i => i.Length) + 4, Math.Max(1, Items.Count));
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (Items.Count == 0)
        {
            return false;
        }

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

            case ConsoleKey.Spacebar:
                Toggle(cursor);
                return true;

            case ConsoleKey.Enter:
                Submitted?.Invoke(CheckedIndices);
                return true;

        }

        return false;
    }

    private void Move(int delta)
    {
        MoveTo((cursor + delta + Items.Count) % Items.Count);
    }

    private void MoveTo(int index)
    {
        Guard.Against.OutOfRange(index, nameof(index), 0, Items.Count - 1);

        cursor = index;
    }

    private void Toggle(int index)
    {
        var isChecked = !CheckedIndices.Remove(index);
        if (isChecked)
        {
            CheckedIndices.Add(index);
        }

        ItemCheckedChanged?.Invoke(index, isChecked);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1)
        {
            return;
        }

        // Keep the cursor in view.
        if (cursor < scroll)
        {
            scroll = cursor;
        }

        if (cursor >= scroll + Bounds.Height)
        {
            scroll = cursor - Bounds.Height + 1;
        }

        for (var row = 0; row < Bounds.Height; row++)
        {
            var i = scroll + row;
            if (i >= Items.Count)
            {
                break;
            }

            var isChecked = CheckedIndices.Contains(i);
            var highlighted = Focused && i == cursor;
            var style = highlighted ? CellStyle.Reverse : CellStyle.None;
            var box = isChecked ? "[x] " : "[ ] ";
            var fg = isChecked ? AccentColor : Foreground;
            var text = box + Items[i];
            if (text.Length > Bounds.Width)
            {
                text = text[..Bounds.Width];
            }

            buffer.Write(Bounds.X, Bounds.Y + row, text.PadRight(Bounds.Width), fg, default, style);
        }
    }
}
