namespace ConsoleRender;

/// <summary>
/// A searchable selection list: a text input on top, the matching items below it. Typing
/// narrows the list, up/down arrows move the highlight and Enter activates the highlighted
/// item; Escape clears the query.
///
/// The whole control is a single Tab stop. The embedded <see cref="Input"/> field stays out
/// of the focus cycle — the search box forwards typing to it and keeps the selection keys
/// for itself, the same way <see cref="ConfirmDialog"/> steers its buttons.
/// </summary>
public class SearchBox : Control
{
    private readonly TextBox input;

    /// <summary>Indices into <see cref="Items"/> of the items matching the current query.</summary>
    private readonly List<int> matches = new();

    private int selectedIndex;
    private int scroll;
    private Func<string, string, bool> filter = DefaultFilter;
    private string emptyText = "";

    public SearchBox()
    {
        Focusable = true;
        input = new TextBox { Focusable = false };
        Add(input);
        input.TextChanged += _ => Refilter();
    }

    public SearchBox(params string[] items) : this()
    {
        Guard.Against.Null(items);
        Items.AddRange(items);
        Refilter();
    }

    /// <summary>All items the search runs over. The list below the input shows the matches.</summary>
    public List<string> Items { get; } = new();

    /// <summary>The embedded input field — for placeholder, colors or a border.</summary>
    public TextBox Input => input;

    /// <summary>The current query text.</summary>
    public string Query => input.Text;

    /// <summary>
    /// Decides whether an item matches a query. Defaults to a case-insensitive substring
    /// match; an empty query always shows every item.
    /// </summary>
    public Func<string, string, bool> Filter
    {
        get => filter;
        set => filter = Guard.Against.Null(value);
    }

    /// <summary>Shown dimmed in place of the list when nothing matches. Empty by default.</summary>
    public string EmptyText
    {
        get => emptyText;
        set => emptyText = Guard.Against.Null(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Magenta;

    /// <summary>The items matching the current query, in item order.</summary>
    public IReadOnlyList<string> Matches
    {
        get
        {
            Refilter();
            return matches.Select(i => Items[i]).ToList();
        }
    }

    /// <summary>The highlighted match, or null when nothing matches.</summary>
    public string? SelectedItem
    {
        get
        {
            Refilter();
            return matches.Count > 0 ? Items[matches[selectedIndex]] : null;
        }
    }

    /// <summary>Raised when Enter activates the highlighted match. Arguments: index into <see cref="Items"/> and the item.</summary>
    public event Action<int, string>? ItemActivated;

    /// <summary>Raised when the highlight moves. Arguments: index into <see cref="Items"/> and the item.</summary>
    public event Action<int, string>? SelectionChanged;

    /// <summary>Rows the input line takes up, including its border if it has one.</summary>
    private int InputHeight => input.BorderMode == BorderMode.None ? 1 : 3;

    protected override Size GetPreferredSize(Size available)
    {
        var width = Math.Max(20, Items.Count == 0 ? 0 : Items.Max(i => i.Length) + 2);
        return new Size(width, InputHeight + Math.Max(1, Items.Count));
    }

    protected override void ArrangeChildren()
    {
        input.Left = 0;
        input.Top = 0;
        input.Right = 0;
        input.Height = InputHeight;
        // The caret belongs to the search box's focus: the inner field never gets its own.
        input.Focused = Focused;
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        Refilter();

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                Move(-1);
                return true;
            case ConsoleKey.DownArrow:
                Move(1);
                return true;
            case ConsoleKey.Enter:
                if (matches.Count > 0)
                {
                    ItemActivated?.Invoke(matches[selectedIndex], Items[matches[selectedIndex]]);
                }

                return true;
            case ConsoleKey.Escape when input.Text.Length > 0:
                input.SetText("");
                return true;
        }

        // Everything else — typing, cursor movement, clipboard — belongs to the input line.
        return input.OnKey(key);
    }

    private void Move(int delta)
    {
        if (matches.Count == 0)
        {
            return;
        }

        selectedIndex = (selectedIndex + delta + matches.Count) % matches.Count;
        SelectionChanged?.Invoke(matches[selectedIndex], Items[matches[selectedIndex]]);
    }

    /// <summary>
    /// Rebuilds <see cref="matches"/> from <see cref="Items"/> and the query, keeping the
    /// highlight on the same item when it still matches. <see cref="Items"/> is freely
    /// mutable, so this runs before every read of the matches rather than only on edits.
    /// </summary>
    private void Refilter()
    {
        var previous = selectedIndex < matches.Count ? matches[selectedIndex] : -1;

        matches.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            if (Query.Length == 0 || filter(Query, Items[i]))
            {
                matches.Add(i);
            }
        }

        selectedIndex = Math.Max(0, matches.IndexOf(previous));
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1)
        {
            return;
        }

        Refilter();

        var top = Bounds.Y + InputHeight;
        var rows = Bounds.Bottom - top;
        if (rows < 1)
        {
            return;
        }

        if (matches.Count == 0)
        {
            if (emptyText.Length > 0)
            {
                buffer.Write(Bounds.X + 2, top, emptyText, Color.DarkGray, default, CellStyle.Italic);
            }

            return;
        }

        // Keep the highlight in view.
        if (selectedIndex < scroll)
        {
            scroll = selectedIndex;
        }

        if (selectedIndex >= scroll + rows)
        {
            scroll = selectedIndex - rows + 1;
        }

        scroll = Math.Clamp(scroll, 0, Math.Max(0, matches.Count - rows));

        for (var row = 0; row < rows; row++)
        {
            var m = scroll + row;
            if (m >= matches.Count)
            {
                break;
            }

            var selected = m == selectedIndex;
            var fg = selected ? AccentColor : Foreground;
            var style = selected && Focused ? CellStyle.Reverse | CellStyle.Bold
                : selected ? CellStyle.Bold
                : CellStyle.None;
            var text = (selected ? "› " : "  ") + Items[matches[m]];
            if (text.Length > Bounds.Width)
            {
                text = text[..Bounds.Width];
            }

            buffer.Write(Bounds.X, top + row, text.PadRight(Bounds.Width), fg, default, style);
        }
    }

    private static bool DefaultFilter(string query, string item)
    {
        return item.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
