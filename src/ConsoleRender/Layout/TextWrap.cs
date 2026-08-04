namespace ConsoleRender;

/// <summary>Word-aware line breaking shared by the dialog controls.</summary>
internal static class TextWrap
{
    /// <summary>
    /// Breaks <paramref name="text"/> into lines of at most <paramref name="width"/> cells.
    /// Existing newlines are kept as paragraph breaks; words longer than the width are
    /// left intact and clipped later by the caller.
    /// </summary>
    public static IReadOnlyList<string> Wrap(string text, int width)
    {
        Guard.Against.Null(text);
        Guard.Against.NegativeOrZero(width);

        var lines = new List<string>();
        foreach (string paragraph in text.Replace("\r", "").Split('\n'))
        {
            if (paragraph.Length == 0)
            {
                lines.Add("");
                continue;
            }

            var current = "";
            foreach (string word in paragraph.Split(' '))
            {
                if (current.Length == 0)
                    current = word;
                else if (current.Length + 1 + word.Length <= width)
                    current += " " + word;
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }
            if (current.Length > 0)
                lines.Add(current);
        }
        return lines;
    }
}
