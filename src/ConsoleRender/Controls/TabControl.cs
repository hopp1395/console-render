namespace ConsoleRender;

/// <summary>
/// A composite of a tab-header row and per-tab content. Unlike <see cref="SearchBox"/> or
/// <see cref="ConfirmDialog"/>, tab content is typically rich — its own focusable controls —
/// so the header is the only thing this control steers itself; content keeps its own
/// independent Tab stops. The header (this control) is one Tab stop; Tab from the header
/// moves into the active tab's first focusable descendant, since only the active tab's
/// content is <see cref="Control.Visible"/> and stays out of the focus tree otherwise. Shift+Tab
/// from inside the content returns to the header, the same way it would leave any other
/// focusable control.
/// </summary>
public class TabControl : Control
{
    private const int HeaderHeight = 1;

    private readonly List<(string Title, Control Content, Color? AccentColor)> tabs = new();
    private int selectedIndex;
    private BorderStyle border = BorderStyle.Single;

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Cyan;
    public Color BorderColor { get; set; } = Color.DarkGray;

    /// <summary>
    /// Frames the content area below the header row. Its top edge also stands in for the
    /// separator rule between header and content.
    /// </summary>
    public BorderStyle Border
    {
        get => border;
        set => border = Guard.Against.Null(value);
    }

    /// <summary>Titles of every tab, in display order.</summary>
    public IReadOnlyList<string> Titles => tabs.Select(t => t.Title).ToList();

    public int TabCount => tabs.Count;

    /// <summary>Which tab is active. Setting it hides the previous tab's content and shows the new one's.</summary>
    public int SelectedIndex
    {
        get => selectedIndex;
        set
        {
            if (tabs.Count == 0) { selectedIndex = 0; return; }
            Guard.Against.OutOfRange(value, nameof(value), 0, tabs.Count - 1);
            Select(value);
        }
    }

    /// <summary>Raised when the active tab changes, with its index into <see cref="Titles"/>.</summary>
    public event Action<int>? SelectionChanged;

    public TabControl()
    {
        Focusable = true;
    }

    /// <summary>
    /// Adds a tab. <paramref name="content"/> is stretched to fill the area below the header and
    /// stays a child for the control's lifetime — switching tabs only toggles its visibility, so
    /// state (e.g. text typed into a field) survives switching away and back.
    /// </summary>
    /// <param name="title">The tab's header label.</param>
    /// <param name="content">The control shown while this tab is active.</param>
    /// <param name="accentColor">
    /// Overrides <see cref="AccentColor"/> for this tab only, e.g. to mark a tab with an error.
    /// </param>
    public void AddTab(string title, Control content, Color? accentColor = null)
    {
        Guard.Against.NullOrWhiteSpace(title);
        Guard.Against.Null(content);

        var first = tabs.Count == 0;
        content.Left = 0;
        content.Top = 0;
        content.Right = 0;
        content.Bottom = 0;
        content.Visible = first;
        tabs.Add((title, content, accentColor));
        Add(content);
    }

    /// <summary>The bordered box below the header row, before deflating for the border itself.</summary>
    private Rect PanelRect
    {
        get
        {
            var height = Math.Max(0, Bounds.Height - HeaderHeight);
            return new Rect(Bounds.X, Bounds.Y + HeaderHeight, Bounds.Width, height);
        }
    }

    public override Rect ContentRect => PanelRect.Deflate(1);

    protected override Size GetPreferredSize(Size available)
    {
        return new(available.Width, available.Height);
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (tabs.Count == 0)
        {
            return false;
        }

        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                Select((selectedIndex - 1 + tabs.Count) % tabs.Count);
                return true;
            case ConsoleKey.RightArrow:
                Select((selectedIndex + 1) % tabs.Count);
                return true;
            case ConsoleKey.Home:
                Select(0);
                return true;
            case ConsoleKey.End:
                Select(tabs.Count - 1);
                return true;
        }

        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1 || Bounds.Width < 1)
        {
            return;
        }

        var x = Bounds.X;
        for (var i = 0; i < tabs.Count; i++)
        {
            if (x >= Bounds.Right)
            {
                break;
            }

            var active = i == selectedIndex;
            var color = tabs[i].AccentColor ?? AccentColor;
            var fg = active ? color : Foreground;
            var style = active
                ? (Focused ? CellStyle.Reverse | CellStyle.Bold : CellStyle.Bold)
                : CellStyle.None;

            var label = $" {tabs[i].Title} ";
            buffer.Write(x, Bounds.Y, label, fg, Background, style);
            x += label.Length;

            if (i < tabs.Count - 1)
            {
                buffer.Write(x, Bounds.Y, "│", BorderColor, Background);
                x += 1;
            }
        }

        buffer.DrawBorder(PanelRect, Border, BorderColor, Background);
    }

    private void Select(int index)
    {
        if (index == selectedIndex)
        {
            return;
        }

        tabs[selectedIndex].Content.Visible = false;
        selectedIndex = index;
        tabs[selectedIndex].Content.Visible = true;
        SelectionChanged?.Invoke(selectedIndex);
    }
}
