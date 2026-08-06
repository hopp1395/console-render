namespace ConsoleRender;

/// <summary>
/// A modal dialog that asks a question and offers several answers as a row of buttons.
///
/// The dialog owns the selection rather than letting each button take keyboard focus:
/// left/right (or Tab) move along the row, Enter confirms, Escape cancels. That keeps the
/// whole question a single Tab stop and lets the highlight show which answer is preselected.
/// </summary>
public class ConfirmDialog : ModalControl
{
    private readonly List<Button> buttons = new();
    private string title = "";
    private string text = "";
    private BorderStyle border = BorderStyle.Double;
    private int maxTextWidth = 48;
    private int selectedIndex;

    public string Title
    {
        get => title;
        set => title = Guard.Against.Null(value);
    }

    public string Text
    {
        get => text;
        set => text = Guard.Against.Null(value);
    }

    public BorderStyle Border
    {
        get => border;
        set => border = Guard.Against.Null(value);
    }

    public Color BorderColor { get; set; } = Color.Yellow;
    public Color Foreground { get; set; } = Color.White;
    public Color Background { get; set; } = Color.DarkBlue;

    /// <summary>Maximum text width before wrapping.</summary>
    public int MaxTextWidth
    {
        get => maxTextWidth;
        set => maxTextWidth = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>The answers, in the order they appear from left to right.</summary>
    public IReadOnlyList<string> Options => buttons.Select(b => b.Text).ToList();

    /// <summary>Which answer is preselected, and after Enter, which one was chosen.</summary>
    public int SelectedIndex
    {
        get => selectedIndex;
        set => selectedIndex = buttons.Count == 0
            ? 0
            : Guard.Against.OutOfRange(value, nameof(value), 0, buttons.Count - 1);
    }

    /// <summary>Raised with the index and label of the chosen answer, after the dialog closed.</summary>
    public event Action<int, string>? Chosen;

    /// <summary>Raised when the dialog was dismissed with Escape instead of an answer.</summary>
    public event Action? Cancelled;

    public ConfirmDialog() { }

    public ConfirmDialog(string title, string text, params string[] options)
    {
        Title = Guard.Against.Null(title);
        Text = Guard.Against.Null(text);
        SetOptions(options);
    }

    /// <summary>Replaces the answer buttons.</summary>
    public void SetOptions(params string[] options)
    {
        Guard.Against.NullOrEmpty(options);

        foreach (var button in buttons)
            Remove(button);
        buttons.Clear();

        foreach (string option in options)
        {
            Guard.Against.Null(option, nameof(options));
            // The dialog drives the selection, so the buttons stay out of the focus cycle.
            var button = new Button(option) { Focusable = false, Background = Background };
            buttons.Add(button);
            Add(button);
        }

        selectedIndex = 0;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (buttons.Count == 0) return false;

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                Move(-1);
                return true;
            case ConsoleKey.RightArrow:
            case ConsoleKey.Tab:
                Move(1);
                return true;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                int index = selectedIndex;
                string label = buttons[index].Text;
                Close();
                Chosen?.Invoke(index, label);
                return true;
            case ConsoleKey.Escape:
                Close();
                Cancelled?.Invoke();
                return true;
        }
        return false;
    }

    private void Move(int delta)
    {
        selectedIndex = (selectedIndex + delta + buttons.Count) % buttons.Count;
    }

    private IReadOnlyList<string> WrapText(int width)
    {
        return TextWrap.Wrap(Text, width);
    }

    private int ButtonRowWidth()
    {
        return buttons.Count == 0 ? 0 : buttons.Sum(b => b.Text.Length + 4) + (buttons.Count - 1);
    }

    protected override Size GetPreferredSize(Size available)
    {
        int textWidth = Math.Min(MaxTextWidth, Math.Max(10, available.Width - 8));
        var lines = WrapText(textWidth);
        int contentWidth = Math.Max(
            Math.Max(Title.Length + 2, ButtonRowWidth()),
            Math.Max(lines.Count == 0 ? 0 : lines.Max(l => l.Length), 12));
        // Top border, the text, a blank row, the button row, and the bottom border.
        return new Size(contentWidth + 4, lines.Count + 4);
    }

    public override Rect ContentRect => Bounds.Deflate(2, 1);

    protected override void ArrangeChildren()
    {
        var inner = ContentRect;
        if (inner.IsEmpty || buttons.Count == 0) return;

        int rowWidth = ButtonRowWidth();
        int offset = Math.Max(0, (inner.Width - rowWidth) / 2);
        int row = Math.Max(0, inner.Height - 1);

        for (int i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];
            int width = button.Text.Length + 4;
            button.Left = offset;
            button.Top = row;
            button.Width = width;
            button.Height = 1;
            button.Highlighted = i == selectedIndex;
            offset += width + 1;
        }
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        buffer.FillRect(Bounds, ' ', Foreground, Background);
        buffer.DrawBorder(Bounds, Border, BorderColor, Background, Title, Color.Yellow);

        var inner = ContentRect;
        if (inner.Width <= 0) return;

        var lines = WrapText(inner.Width);
        for (int i = 0; i < lines.Count && i < inner.Height; i++)
            buffer.Write(inner.X, inner.Y + i, lines[i], Foreground, Background);
    }
}
