using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A <see cref="TreeView"/> browsing a sample source-file layout.</summary>
internal static class TreeViewPage
{
    public static Panel Build(Label status)
    {
        var tree = new TreeView { Left = 0, Top = 3, Right = 0, Bottom = 0 };
        var src = new TreeNode("src")
        {
            IsExpanded = true,
            Children =
            {
                new TreeNode("Controls")
                {
                    Children = { new TreeNode("Table.cs"), new TreeNode("TreeView.cs"), new TreeNode("TreeNode.cs") },
                },
                new TreeNode("Core")
                {
                    Children = { new TreeNode("ConsoleBuffer.cs"), new TreeNode("Renderer.cs") },
                },
            },
        };

        var tests = new TreeNode("tests") { Children = { new TreeNode("TreeViewTests.cs") } };
        tree.Roots.Add(src);
        tree.Roots.Add(tests);

        tree.SelectionChanged += node => status.Text = node is null ? "" : $"Highlighted: {node.Text}";
        tree.NodeActivated += node => status.Text = $"Activated: {node.Text}";

        return Fill(
            Info(0, "Left/Right collapse or expand a node, or step to its parent/first child."),
            Info(1, "Space toggles, Enter activates. Up/Down move across visible nodes only."),
            tree);
    }
}
