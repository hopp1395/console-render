namespace ConsoleRender;

/// <summary>A key together with its modifiers, e.g. Ctrl+Q.</summary>
public readonly record struct KeyCombo(ConsoleKey Key, ConsoleModifiers Modifiers = 0)
{
    public static KeyCombo Ctrl(ConsoleKey key)
    {
        return new(key, ConsoleModifiers.Control);
    }

    public static KeyCombo Alt(ConsoleKey key)
    {
        return new(key, ConsoleModifiers.Alt);
    }

    public static KeyCombo Shift(ConsoleKey key)
    {
        return new(key, ConsoleModifiers.Shift);
    }

    public static KeyCombo FromKeyInfo(ConsoleKeyInfo info)
    {
        return new(info.Key, info.Modifiers);
    }

    public override string ToString()
    {
        var parts = new List<string>(4);
        if (Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ConsoleModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}
