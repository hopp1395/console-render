namespace ConsoleRender.Tests;

public class TreeViewTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0')
    {
        return new(ch, key, false, false, false);
    }

    private static TreeView SampleTree()
    {
        var tree = new TreeView();
        var folder = new TreeNode("Folder")
        {
            IsExpanded = true,
            Children =
            {
                new TreeNode("FileA"),
                new TreeNode("FileB"),
            },
        };
        tree.Roots.Add(folder);
        tree.Roots.Add(new TreeNode("Root2"));
        return tree;
    }

    [Fact]
    public void HighlightedNode_DefaultsToTheFirstRoot()
    {
        var tree = SampleTree();

        tree.OnKey(Key(ConsoleKey.Home));

        Assert.Equal("Folder", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void DownArrowMovesThroughVisibleNodesOnly()
    {
        var tree = new TreeView();
        var collapsed = new TreeNode("Collapsed") { Children = { new TreeNode("Hidden") } };
        tree.Roots.Add(collapsed);
        tree.Roots.Add(new TreeNode("Next"));

        tree.OnKey(Key(ConsoleKey.DownArrow));

        Assert.Equal("Next", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void ArrowKeysClampAtTheEnds()
    {
        var tree = SampleTree();

        tree.OnKey(Key(ConsoleKey.UpArrow));
        Assert.Equal("Folder", tree.HighlightedNode?.Text);

        tree.OnKey(Key(ConsoleKey.End));
        Assert.Equal("Root2", tree.HighlightedNode?.Text);

        tree.OnKey(Key(ConsoleKey.DownArrow));
        Assert.Equal("Root2", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void HomeAndEndJumpToTheFirstAndLastVisibleNode()
    {
        var tree = SampleTree();

        tree.OnKey(Key(ConsoleKey.End));
        Assert.Equal("Root2", tree.HighlightedNode?.Text);

        tree.OnKey(Key(ConsoleKey.Home));
        Assert.Equal("Folder", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void RightArrowExpandsThenMovesIntoTheFirstChild()
    {
        var tree = new TreeView();
        tree.Roots.Add(new TreeNode("Folder") { Children = { new TreeNode("Child") } });

        tree.OnKey(Key(ConsoleKey.RightArrow));
        Assert.True(tree.Roots[0].IsExpanded);
        Assert.Equal("Folder", tree.HighlightedNode?.Text);

        tree.OnKey(Key(ConsoleKey.RightArrow));
        Assert.Equal("Child", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void RightArrowOnALeafDoesNothing()
    {
        var tree = new TreeView();
        tree.Roots.Add(new TreeNode("Leaf"));

        tree.OnKey(Key(ConsoleKey.RightArrow));

        Assert.Equal("Leaf", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void LeftArrowCollapsesThenMovesToTheParent()
    {
        var tree = SampleTree();

        tree.OnKey(Key(ConsoleKey.DownArrow)); // Folder -> FileA
        tree.OnKey(Key(ConsoleKey.LeftArrow)); // no children/not expanded -> jump to parent
        Assert.Equal("Folder", tree.HighlightedNode?.Text);

        tree.OnKey(Key(ConsoleKey.LeftArrow)); // expanded root -> collapses in place
        Assert.True(tree.Roots[0].IsExpanded == false);
        Assert.Equal("Folder", tree.HighlightedNode?.Text);
    }

    [Fact]
    public void SpaceTogglesExpansionOfANodeWithChildren()
    {
        var tree = SampleTree();

        tree.OnKey(Key(ConsoleKey.Spacebar, ' '));
        Assert.False(tree.Roots[0].IsExpanded);

        tree.OnKey(Key(ConsoleKey.Spacebar, ' '));
        Assert.True(tree.Roots[0].IsExpanded);
    }

    [Fact]
    public void SpaceOnALeafDoesNothing()
    {
        var tree = new TreeView();
        tree.Roots.Add(new TreeNode("Leaf"));

        Assert.True(tree.OnKey(Key(ConsoleKey.Spacebar, ' ')));
        Assert.Empty(tree.Roots[0].Children);
    }

    [Fact]
    public void SelectionChanged_FiresOnlyWhenTheHighlightActuallyMoves()
    {
        var tree = SampleTree();
        var fired = new List<string?>();
        tree.SelectionChanged += node => fired.Add(node?.Text);

        tree.OnKey(Key(ConsoleKey.UpArrow)); // already at the first node, clamps, no change
        tree.OnKey(Key(ConsoleKey.DownArrow));

        Assert.Equal(new[] { "FileA" }, fired);
    }

    [Fact]
    public void NodeActivated_FiresWithTheHighlightedNodeOnEnter()
    {
        var tree = SampleTree();
        TreeNode? activated = null;
        tree.NodeActivated += node => activated = node;

        tree.OnKey(Key(ConsoleKey.Enter));

        Assert.Same(tree.Roots[0], activated);
    }

    [Fact]
    public void EmptyTree_IgnoresKeys()
    {
        var tree = new TreeView();

        Assert.False(tree.OnKey(Key(ConsoleKey.DownArrow)));
    }

    [Fact]
    public void RendersIndentAndExpandGlyphsForVisibleNodes()
    {
        var tree = SampleTree();
        tree.Left = 0;
        tree.Top = 0;
        tree.Width = 12;
        tree.Height = 3;

        using var app = new ConsoleApp();
        app.Root.Add(tree);

        var lines = app.RenderOffscreen(12, 3).ToText().Split('\n');

        Assert.StartsWith("▾ Folder", lines[0]);
        Assert.StartsWith("    FileA", lines[1]);
        Assert.StartsWith("    FileB", lines[2]);
    }

    [Fact]
    public void CollapsedNodeShowsAClosedGlyphAndHidesItsChildren()
    {
        var tree = new TreeView();
        tree.Roots.Add(new TreeNode("Folder") { Children = { new TreeNode("Hidden") } });
        tree.Left = 0;
        tree.Top = 0;
        tree.Width = 12;
        tree.Height = 2;

        using var app = new ConsoleApp();
        app.Root.Add(tree);

        var text = app.RenderOffscreen(12, 2).ToText();

        Assert.Contains("▸ Folder", text);
        Assert.DoesNotContain("Hidden", text);
    }
}
