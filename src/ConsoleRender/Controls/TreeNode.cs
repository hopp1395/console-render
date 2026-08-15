namespace ConsoleRender;

/// <summary>A node in a <see cref="TreeView"/>, with its own children and expand state.</summary>
public sealed class TreeNode
{
    private string text;

    public TreeNode(string text)
    {
        Text = Guard.Against.Null(text);
    }

    public string Text
    {
        get => text;
        set => text = Guard.Against.Null(value);
    }

    public List<TreeNode> Children { get; } = new();

    public bool IsExpanded { get; set; }
}
