namespace ConsoleRender;

/// <summary>
/// A scrollable, expandable tree. Up/Down move the highlight across the currently visible
/// (expanded) nodes, Left collapses the highlighted node or moves to its parent, Right expands
/// it or moves to its first child, Space toggles expand/collapse and Enter raises
/// <see cref="NodeActivated"/> — moving the highlight alone activates nothing.
/// </summary>
public class TreeView : Control
{
    private int scroll;
    private TreeNode? highlighted;

    public List<TreeNode> Roots { get; } = new();

    public TreeNode? HighlightedNode => highlighted;

    public Color Foreground { get; set; } = Color.Default;
    public Color AccentColor { get; set; } = Color.Cyan;

    /// <summary>Raised when the highlight moves to a different node.</summary>
    public event Action<TreeNode?>? SelectionChanged;

    /// <summary>Raised when Enter is pressed on the highlighted node.</summary>
    public event Action<TreeNode>? NodeActivated;

    public TreeView()
    {
        Focusable = true;
    }

    protected override Size GetPreferredSize(Size available)
    {
        var flat = Flatten();
        if (flat.Count == 0)
        {
            return new(8, 1);
        }

        var width = flat.Max(e => e.Depth * 2 + 2 + e.Node.Text.Length);
        return new(width, flat.Count);
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (Roots.Count == 0)
        {
            return false;
        }

        var flat = Flatten();
        var index = ResolveIndex(flat);

        switch (key.Key)
        {

            case ConsoleKey.UpArrow:
                MoveTo(flat, Math.Max(0, index - 1));
                return true;

            case ConsoleKey.DownArrow:
                MoveTo(flat, Math.Min(flat.Count - 1, index + 1));
                return true;

            case ConsoleKey.Home:
                MoveTo(flat, 0);
                return true;

            case ConsoleKey.End:
                MoveTo(flat, flat.Count - 1);
                return true;

            case ConsoleKey.LeftArrow:
                CollapseOrMoveToParent(flat, index);
                return true;

            case ConsoleKey.RightArrow:
                ExpandOrMoveToFirstChild(flat, index);
                return true;

            case ConsoleKey.Spacebar:
                Toggle(flat[index].Node);
                return true;

            case ConsoleKey.Enter:
                NodeActivated?.Invoke(flat[index].Node);
                return true;

        }

        return false;
    }

    private int ResolveIndex(List<(TreeNode Node, int Depth)> flat)
    {
        if (flat.Count == 0)
        {
            highlighted = null;
            return -1;
        }

        var index = highlighted is null ? -1 : flat.FindIndex(e => e.Node == highlighted);
        if (index < 0)
        {
            index = 0;
            highlighted = flat[0].Node;
        }

        return index;
    }

    private void MoveTo(List<(TreeNode Node, int Depth)> flat, int index)
    {
        var node = flat[index].Node;
        if (node == highlighted)
        {
            return;
        }

        highlighted = node;
        SelectionChanged?.Invoke(highlighted);
    }

    private void CollapseOrMoveToParent(List<(TreeNode Node, int Depth)> flat, int index)
    {
        var (node, depth) = flat[index];
        if (node.Children.Count > 0 && node.IsExpanded)
        {
            node.IsExpanded = false;
            return;
        }

        for (var i = index - 1; i >= 0; i--)
        {
            if (flat[i].Depth < depth)
            {
                MoveTo(flat, i);
                return;
            }
        }
    }

    private void ExpandOrMoveToFirstChild(List<(TreeNode Node, int Depth)> flat, int index)
    {
        var (node, depth) = flat[index];
        if (node.Children.Count == 0)
        {
            return;
        }

        if (!node.IsExpanded)
        {
            node.IsExpanded = true;
            return;
        }

        if (index + 1 < flat.Count && flat[index + 1].Depth > depth)
        {
            MoveTo(flat, index + 1);
        }
    }

    private static void Toggle(TreeNode node)
    {
        if (node.Children.Count > 0)
        {
            node.IsExpanded = !node.IsExpanded;
        }
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Height < 1 || Roots.Count == 0)
        {
            return;
        }

        var flat = Flatten();
        var index = ResolveIndex(flat);

        // Keep the highlight in view.
        if (index < scroll)
        {
            scroll = index;
        }

        if (index >= scroll + Bounds.Height)
        {
            scroll = index - Bounds.Height + 1;
        }

        for (var row = 0; row < Bounds.Height; row++)
        {
            var i = scroll + row;
            if (i >= flat.Count)
            {
                break;
            }

            var (node, depth) = flat[i];
            var glyph = node.Children.Count == 0 ? "  " : node.IsExpanded ? "▾ " : "▸ ";
            var text = new string(' ', depth * 2) + glyph + node.Text;
            if (text.Length > Bounds.Width)
            {
                text = text[..Bounds.Width];
            }

            var selected = i == index;
            var style = selected && Focused ? CellStyle.Reverse | CellStyle.Bold
                : selected ? CellStyle.Bold
                : CellStyle.None;
            var fg = selected ? AccentColor : Foreground;
            buffer.Write(Bounds.X, Bounds.Y + row, text.PadRight(Bounds.Width), fg, default, style);
        }
    }

    private List<(TreeNode Node, int Depth)> Flatten()
    {
        var result = new List<(TreeNode, int)>();
        foreach (var root in Roots)
        {
            AppendNode(root, 0, result);
        }

        return result;
    }

    private static void AppendNode(TreeNode node, int depth, List<(TreeNode, int)> result)
    {
        result.Add((node, depth));
        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                AppendNode(child, depth + 1, result);
            }
        }
    }
}
