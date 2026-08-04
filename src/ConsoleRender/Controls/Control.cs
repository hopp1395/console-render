namespace ConsoleRender;

/// <summary>
/// Base class of all UI elements.
///
/// Layout works with anchors, similar to CSS absolute positioning:
/// <list type="bullet">
/// <item><see cref="Left"/>/<see cref="Right"/> are distances to the parent's edges.
/// Setting both stretches the control horizontally on resize.</item>
/// <item>If neither is set, <see cref="HorizontalAlignment"/> positions the control.</item>
/// <item>The same applies vertically with <see cref="Top"/>/<see cref="Bottom"/> and
/// <see cref="VerticalAlignment"/>.</item>
/// <item><see cref="Width"/>/<see cref="Height"/> override the control's preferred size.</item>
/// </list>
/// </summary>
public abstract class Control
{
    private readonly List<Control> _children = new();

    public int? Left { get; set; }
    public int? Top { get; set; }
    public int? Right { get; set; }
    public int? Bottom { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    public bool Visible { get; set; } = true;

    /// <summary>Whether the control participates in Tab focus cycling and receives key input.</summary>
    public bool Focusable { get; protected set; }

    /// <summary>True while this control has keyboard focus. Managed by <see cref="ConsoleApp"/>.</summary>
    public bool Focused { get; internal set; }

    /// <summary>Absolute screen bounds, computed by the last layout pass.</summary>
    public Rect Bounds { get; private set; }

    public Control? Parent { get; private set; }

    public IReadOnlyList<Control> Children => _children;

    /// <summary>Area available to child controls (e.g. inside a frame's border).</summary>
    public virtual Rect ContentRect => Bounds;

    public void Add(Control child)
    {
        Guard.Against.Null(child);
        if (child.Parent is not null)
            throw new InvalidOperationException("Control already has a parent.");
        if (ReferenceEquals(child, this))
            throw new InvalidOperationException("A control cannot be its own child.");

        child.Parent = this;
        _children.Add(child);
    }

    /// <summary>Adds several children in one call.</summary>
    public void AddRange(params Control[] children)
    {
        Guard.Against.Null(children);
        foreach (var child in children)
            Add(child);
    }

    public void Remove(Control child)
    {
        Guard.Against.Null(child);

        if (_children.Remove(child))
            child.Parent = null;
    }

    /// <summary>Computes <see cref="Bounds"/> from the anchors within <paramref name="area"/> and lays out children.</summary>
    public void PerformLayout(Rect area)
    {
        if (!Visible) return;

        var preferred = GetPreferredSize(new Size(area.Width, area.Height));

        int w = Width ?? (Left.HasValue && Right.HasValue
            ? area.Width - Left.Value - Right.Value
            : preferred.Width);
        int h = Height ?? (Top.HasValue && Bottom.HasValue
            ? area.Height - Top.Value - Bottom.Value
            : preferred.Height);
        w = Math.Max(0, w);
        h = Math.Max(0, h);

        int x = Left.HasValue ? area.X + Left.Value
            : Right.HasValue ? area.Right - Right.Value - w
            : HorizontalAlignment switch
            {
                HorizontalAlignment.Center => area.X + (area.Width - w) / 2,
                HorizontalAlignment.Right => area.Right - w,
                _ => area.X,
            };

        int y = Top.HasValue ? area.Y + Top.Value
            : Bottom.HasValue ? area.Bottom - Bottom.Value - h
            : VerticalAlignment switch
            {
                VerticalAlignment.Middle => area.Y + (area.Height - h) / 2,
                VerticalAlignment.Bottom => area.Bottom - h,
                _ => area.Y,
            };

        Bounds = new Rect(x, y, w, h);

        foreach (var child in _children)
            child.PerformLayout(ContentRect);
    }

    /// <summary>Natural size of the control when no explicit size or stretching anchors are set.</summary>
    protected virtual Size GetPreferredSize(Size available) => new(10, 1);

    internal void Render(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (!Visible || Bounds.IsEmpty) return;

        // A control may only paint inside its own bounds, and its children only inside
        // the content area — so an oversized child can never bleed over its parent.
        buffer.PushClip(Bounds);
        try
        {
            Draw(buffer);
        }
        finally
        {
            buffer.PopClip();
        }

        if (_children.Count == 0) return;

        buffer.PushClip(ContentRect);
        try
        {
            foreach (var child in _children)
                child.Render(buffer);
        }
        finally
        {
            buffer.PopClip();
        }
    }

    internal void UpdateAll(TimeSpan delta)
    {
        Guard.Against.Negative(delta);

        if (!Visible) return;
        Update(delta);
        foreach (var child in _children)
            child.UpdateAll(delta);
    }

    /// <summary>Draws this control (not its children) into the buffer.</summary>
    protected abstract void Draw(ConsoleBuffer buffer);

    /// <summary>Advances animations. Called once per frame.</summary>
    public virtual void Update(TimeSpan delta) { }

    /// <summary>Handles a key press while focused. Return true if the key was consumed.</summary>
    public virtual bool OnKey(ConsoleKeyInfo key) => false;

    internal void CollectFocusable(List<Control> result)
    {
        Guard.Against.Null(result);

        if (!Visible) return;
        if (Focusable) result.Add(this);
        foreach (var child in _children)
            child.CollectFocusable(result);
    }
}
