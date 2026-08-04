namespace ConsoleRender;

/// <summary>A checkbox toggled with Space or Enter.</summary>
public class Checkbox : Control
{
    private string _text = "";

    public string Text
    {
        get => _text;
        set => _text = Guard.Against.Null(value);
    }

    public bool Checked { get; set; }
    public Color Foreground { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Green;

    public event Action<bool>? CheckedChanged;

    public Checkbox() => Focusable = true;

    public Checkbox(string text) : this() => Text = Guard.Against.Null(text);

    protected override Size GetPreferredSize(Size available) => new(Text.Length + 4, 1);

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Spacebar or ConsoleKey.Enter)
        {
            Checked = !Checked;
            CheckedChanged?.Invoke(Checked);
            return true;
        }
        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        var style = Focused ? CellStyle.Reverse : CellStyle.None;
        string box = Checked ? "[x] " : "[ ] ";
        buffer.Write(Bounds.X, Bounds.Y, box, Checked ? AccentColor : Foreground, default, style);
        buffer.Write(Bounds.X + 4, Bounds.Y, Text, Foreground, default, style);
    }
}
