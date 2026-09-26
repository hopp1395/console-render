namespace ConsoleRender;

/// <summary>How a <see cref="DiffView"/> arranges old and new text.</summary>
public enum DiffLayout
{
    /// <summary>Side by side when the view is at least <see cref="DiffView.SideBySideMinWidth"/> wide, else unified.</summary>
    Auto,

    /// <summary>Old text on the left, new text on the right, changed lines paired up.</summary>
    SideBySide,

    /// <summary>One column in diff order, like <c>git diff</c> prints it.</summary>
    Unified,
}
