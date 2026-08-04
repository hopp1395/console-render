namespace ConsoleRender;

/// <summary>
/// A single-line text input field with cursor, horizontal scrolling, clipboard support
/// (Ctrl+C copies the text, Ctrl+V pastes) and a Submitted event on Enter.
/// </summary>
public class TextBox : Control
{
    private int cursor;
    private int scroll;

    private string placeholder = "";
    private BorderStyle border = BorderStyle.Single;

    public string Text { get; private set; } = "";

    /// <summary>
    /// Whether and how the field frames itself. A border needs three rows — and, when closed,
    /// two extra columns — which <see cref="Control.GetPreferredSize"/> accounts for. If the
    /// control is too small for the chosen border, the text wins and the border is left out.
    /// </summary>
    public BorderMode BorderMode { get; set; } = BorderMode.None;

    /// <summary>Characters used for the border. Only takes effect with a <see cref="BorderMode"/>.</summary>
    public BorderStyle Border
    {
        get => border;
        set => border = Guard.Against.Null(value);
    }

    public Color BorderColor { get; set; } = Color.Default;

    /// <summary>Hint text shown while the box is empty and unfocused.</summary>
    public string Placeholder
    {
        get => placeholder;
        set => placeholder = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Rgb(38, 38, 46);
    public Color PlaceholderColor { get; set; } = Color.DarkGray;

    /// <summary>Raised when Enter is pressed. The argument is the current text.</summary>
    public event Action<string>? Submitted;

    /// <summary>Raised whenever the text changes.</summary>
    public event Action<string>? TextChanged;

    public TextBox() => Focusable = true;

    protected override Size GetPreferredSize(Size available) => BorderMode switch
    {
        BorderMode.Full => new Size(22, 3),
        BorderMode.TopAndBottom => new Size(20, 3),
        _ => new Size(20, 1),
    };

    /// <summary>True when the current bounds leave room for the chosen border.</summary>
    private bool BorderFits => BorderMode switch
    {
        BorderMode.Full => Bounds.Height >= 3 && Bounds.Width >= 3,
        BorderMode.TopAndBottom => Bounds.Height >= 3,
        _ => false,
    };

    /// <summary>The single row the text is drawn on, inside the border if there is one.</summary>
    private Rect TextRect => !BorderFits
        ? new Rect(Bounds.X, Bounds.Y, Bounds.Width, 1)
        : BorderMode == BorderMode.Full
            ? new Rect(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 2, 1)
            : new Rect(Bounds.X, Bounds.Y + 1, Bounds.Width, 1);

    public void SetText(string text)
    {
        Guard.Against.Null(text);

        Text = text;
        cursor = text.Length;
        TextChanged?.Invoke(Text);
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                OnSubmit(Text);
                return true;
            case ConsoleKey.LeftArrow:
                cursor = Math.Max(0, cursor - 1);
                return true;
            case ConsoleKey.RightArrow:
                cursor = Math.Min(Text.Length, cursor + 1);
                return true;
            case ConsoleKey.Home:
                cursor = 0;
                return true;
            case ConsoleKey.End:
                cursor = Text.Length;
                return true;
            case ConsoleKey.Backspace:
                if (cursor > 0)
                {
                    Text = Text.Remove(cursor - 1, 1);
                    cursor--;
                    TextChanged?.Invoke(Text);
                }
                return true;
            case ConsoleKey.Delete:
                if (cursor < Text.Length)
                {
                    Text = Text.Remove(cursor, 1);
                    TextChanged?.Invoke(Text);
                }
                return true;
            case ConsoleKey.U when ctrl:
                Text = "";
                cursor = 0;
                TextChanged?.Invoke(Text);
                return true;
            case ConsoleKey.C when ctrl:
                if (Text.Length > 0)
                    Clipboard.TrySetText(Text);
                return true;
            case ConsoleKey.V when ctrl:
                if (Clipboard.TryGetText(out string pasted))
                    Insert(pasted.Replace("\r", "").Replace('\n', ' '));
                return true;
        }

        if (!ctrl && key.KeyChar >= ' ' && key.KeyChar != '\x7f')
        {
            Insert(key.KeyChar.ToString());
            return true;
        }

        return false;
    }

    /// <summary>Invoked on Enter; subclasses can override to intercept submission.</summary>
    protected virtual void OnSubmit(string text)
    {
        Guard.Against.Null(text);

        Submitted?.Invoke(text);
    }

    protected void RaiseSubmitted(string text)
    {
        Guard.Against.Null(text);

        Submitted?.Invoke(text);
    }

    private void Insert(string s)
    {
        Guard.Against.Null(s);

        Text = Text.Insert(cursor, s);
        cursor += s.Length;
        TextChanged?.Invoke(Text);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1) return;

        DrawBorder(buffer);

        var area = TextRect;
        if (area.Width < 1) return;

        buffer.FillRect(area, ' ', Foreground, Background);

        // Keep the cursor visible by scrolling horizontally.
        int visible = Math.Max(1, area.Width - 1);
        if (cursor < scroll) scroll = cursor;
        if (cursor > scroll + visible) scroll = cursor - visible;
        scroll = Math.Clamp(scroll, 0, Math.Max(0, Text.Length - 1));

        if (Text.Length == 0 && !Focused && Placeholder.Length > 0)
        {
            buffer.Write(area.X, area.Y, Truncate(Placeholder, area.Width),
                PlaceholderColor, Background, CellStyle.Italic);
            return;
        }

        string view = Text.Length > scroll ? Text[scroll..] : "";
        buffer.Write(area.X, area.Y, Truncate(view, area.Width), Foreground, Background);

        if (Focused)
        {
            int cx = area.X + (cursor - scroll);
            if (cx >= area.X && cx < area.Right)
            {
                char under = cursor < Text.Length ? Text[cursor] : ' ';
                buffer.Set(cx, area.Y, under, Foreground, Background, CellStyle.Reverse);
            }
        }
    }

    private void DrawBorder(ConsoleBuffer buffer)
    {
        if (!BorderFits) return;

        if (BorderMode == BorderMode.Full)
        {
            buffer.DrawBorder(Bounds, Border, BorderColor);
            return;
        }

        for (int x = Bounds.X; x < Bounds.Right; x++)
        {
            buffer.Set(x, Bounds.Y, Border.Horizontal, BorderColor);
            buffer.Set(x, Bounds.Bottom - 1, Border.Horizontal, BorderColor);
        }
    }

    private static string Truncate(string s, int max)
    {
        Guard.Against.Null(s);
        Guard.Against.Negative(max);

        return s.Length > max ? s[..max] : s;
    }
}
