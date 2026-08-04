namespace ConsoleRender;

/// <summary>
/// A single-line text input field with cursor, horizontal scrolling, clipboard support
/// (Ctrl+C copies the text, Ctrl+V pastes) and a Submitted event on Enter.
/// </summary>
public class TextBox : Control
{
    private int _cursor;
    private int _scroll;

    private string _placeholder = "";

    public string Text { get; private set; } = "";

    /// <summary>Hint text shown while the box is empty and unfocused.</summary>
    public string Placeholder
    {
        get => _placeholder;
        set => _placeholder = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Rgb(38, 38, 46);
    public Color PlaceholderColor { get; set; } = Color.DarkGray;

    /// <summary>Raised when Enter is pressed. The argument is the current text.</summary>
    public event Action<string>? Submitted;

    /// <summary>Raised whenever the text changes.</summary>
    public event Action<string>? TextChanged;

    public TextBox() => Focusable = true;

    protected override Size GetPreferredSize(Size available) => new(20, 1);

    public void SetText(string text)
    {
        Guard.Against.Null(text);

        Text = text;
        _cursor = text.Length;
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
                _cursor = Math.Max(0, _cursor - 1);
                return true;
            case ConsoleKey.RightArrow:
                _cursor = Math.Min(Text.Length, _cursor + 1);
                return true;
            case ConsoleKey.Home:
                _cursor = 0;
                return true;
            case ConsoleKey.End:
                _cursor = Text.Length;
                return true;
            case ConsoleKey.Backspace:
                if (_cursor > 0)
                {
                    Text = Text.Remove(_cursor - 1, 1);
                    _cursor--;
                    TextChanged?.Invoke(Text);
                }
                return true;
            case ConsoleKey.Delete:
                if (_cursor < Text.Length)
                {
                    Text = Text.Remove(_cursor, 1);
                    TextChanged?.Invoke(Text);
                }
                return true;
            case ConsoleKey.U when ctrl:
                Text = "";
                _cursor = 0;
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

        Text = Text.Insert(_cursor, s);
        _cursor += s.Length;
        TextChanged?.Invoke(Text);
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1) return;

        buffer.FillRect(new Rect(Bounds.X, Bounds.Y, Bounds.Width, 1), ' ', Foreground, Background);

        // Keep the cursor visible by scrolling horizontally.
        int visible = Math.Max(1, Bounds.Width - 1);
        if (_cursor < _scroll) _scroll = _cursor;
        if (_cursor > _scroll + visible) _scroll = _cursor - visible;
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, Text.Length - 1));

        if (Text.Length == 0 && !Focused && Placeholder.Length > 0)
        {
            buffer.Write(Bounds.X, Bounds.Y, Truncate(Placeholder, Bounds.Width),
                PlaceholderColor, Background, CellStyle.Italic);
            return;
        }

        string view = Text.Length > _scroll ? Text[_scroll..] : "";
        buffer.Write(Bounds.X, Bounds.Y, Truncate(view, Bounds.Width), Foreground, Background);

        if (Focused)
        {
            int cx = Bounds.X + (_cursor - _scroll);
            if (cx >= Bounds.X && cx < Bounds.Right)
            {
                char under = _cursor < Text.Length ? Text[_cursor] : ' ';
                buffer.Set(cx, Bounds.Y, under, Foreground, Background, CellStyle.Reverse);
            }
        }
    }

    private static string Truncate(string s, int max)
    {
        Guard.Against.Null(s);
        Guard.Against.Negative(max);

        return s.Length > max ? s[..max] : s;
    }
}
