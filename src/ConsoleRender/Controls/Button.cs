namespace ConsoleRender;

/// <summary>
/// A labelled action, drawn as <c>[ Text ]</c> and triggered with Enter or Space.
///
/// In a keyboard-driven interface a button is worth its Tab stop mainly where the choices
/// themselves need to be visible — the answer row of a dialog, or a form used rarely enough
/// that a shortcut would have to be looked up. For anything done often, register a
/// <see cref="KeyBindingManager"/> shortcut instead; it costs no screen space and no Tab stop.
/// </summary>
public class Button : Control
{
    private string text = "";

    public string Text
    {
        get => text;
        set => text = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Yellow;

    /// <summary>
    /// Draws the button as selected without giving it keyboard focus. Containers that steer
    /// the selection themselves — such as <see cref="ConfirmDialog"/> — use this instead of focus.
    /// </summary>
    public bool Highlighted { get; set; }

    /// <summary>Raised when the button is triggered.</summary>
    public event Action? Clicked;

    public Button()
    {
        Focusable = true;
    }

    public Button(string text)
        : this()
    {
        Text = Guard.Against.Null(text);
    }

    /// <summary>Triggers the button as if it had been pressed.</summary>
    public void PerformClick()
    {
        Clicked?.Invoke();
    }

    protected override Size GetPreferredSize(Size available)
    {
        return new(Text.Length + 4, 1);
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Enter or ConsoleKey.Spacebar)
        {
            PerformClick();
            return true;
        }
        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1 || Bounds.Width < 1) return;

        bool active = Focused || Highlighted;
        var style = active ? CellStyle.Reverse | CellStyle.Bold : CellStyle.None;
        var fg = active ? AccentColor : Foreground;

        string label = $"[ {Text} ]";
        if (label.Length > Bounds.Width)
            label = label[..Bounds.Width];

        buffer.Write(Bounds.X, Bounds.Y, label.PadRight(Bounds.Width), fg, Background, style);
    }
}
