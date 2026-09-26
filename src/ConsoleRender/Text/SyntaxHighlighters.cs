namespace ConsoleRender;

/// <summary>Picks a highlighter by name, as used for the info string of Markdown code fences.</summary>
public static class SyntaxHighlighters
{
    /// <summary>
    /// Returns a highlighter for the first word of a fence info string ("csharp",
    /// "json title=x" …), or null when the language is unknown or the string is empty.
    /// </summary>
    public static ISyntaxHighlighter? ForFence(string info)
    {
        Guard.Against.Null(info);

        var trimmed = info.Trim();
        var space = trimmed.IndexOf(' ');
        var name = space < 0 ? trimmed : trimmed[..space];
        if (name.Length == 0)
        {
            return null;
        }

        return CodeLanguage.TryFind(name, out var language) ? new CodeHighlighter(language) : null;
    }
}
