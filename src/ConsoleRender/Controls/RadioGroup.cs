namespace ConsoleRender;

/// <summary>
/// A vertical group of radio buttons. Arrow keys move the cursor,
/// Space/Enter selects the highlighted option.
/// </summary>
public class RadioGroup : Control
{
    private int cursor;

    public List<string> Items { get; } = new();
    public int SelectedIndex { get; set; }
    public Color Foreground { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Cyan;

    public event Action<int>? SelectionChanged;

    public RadioGroup() => Focusable = true;

    public RadioGroup(params string[] items) : this()
    {
        Guard.Against.Null(items);
        Items.AddRange(items);
    }

    public string? SelectedItem =>
        SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    protected override Size GetPreferredSize(Size available) =>
        new(Items.Count == 0 ? 4 : Items.Max(i => i.Length) + 4, Math.Max(1, Items.Count));

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (Items.Count == 0) return false;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                cursor = (cursor - 1 + Items.Count) % Items.Count;
                return true;
            case ConsoleKey.DownArrow:
                cursor = (cursor + 1) % Items.Count;
                return true;
            case ConsoleKey.Spacebar:
            case ConsoleKey.Enter:
                if (SelectedIndex != cursor)
                {
                    SelectedIndex = cursor;
                    SelectionChanged?.Invoke(SelectedIndex);
                }
                return true;
        }
        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        for (int i = 0; i < Items.Count && i < Bounds.Height; i++)
        {
            bool selected = i == SelectedIndex;
            bool highlighted = Focused && i == cursor;
            var style = highlighted ? CellStyle.Reverse : CellStyle.None;
            string bullet = selected ? "(•) " : "( ) ";
            buffer.Write(Bounds.X, Bounds.Y + i, bullet, selected ? AccentColor : Foreground, default, style);
            buffer.Write(Bounds.X + 4, Bounds.Y + i, Items[i], Foreground, default, style);
        }
    }
}
