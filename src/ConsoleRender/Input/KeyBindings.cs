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
        if (Modifiers.HasFlag(ConsoleModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ConsoleModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ConsoleModifiers.Shift)) parts.Add("Shift");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

/// <summary>A registered global key binding.</summary>
public sealed record KeyBinding(KeyCombo Combo, string Description, Action Handler);

/// <summary>
/// Global keyboard shortcuts. Bindings are checked before the focused control sees the key,
/// so they work regardless of which control is active.
/// </summary>
public sealed class KeyBindingManager
{
    private readonly Dictionary<KeyCombo, KeyBinding> bindings = new();

    public IEnumerable<KeyBinding> All => bindings.Values.OrderBy(b => b.Combo.ToString(), StringComparer.Ordinal);

    public void Register(KeyCombo combo, string description, Action handler)
    {
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.Null(handler);

        bindings[combo] = new KeyBinding(combo, description, handler);
    }

    public void Register(ConsoleKey key, string description, Action handler)
    {
        Register(new KeyCombo(key), description, handler);
    }

    public bool Unregister(KeyCombo combo)
    {
        return bindings.Remove(combo);
    }

    /// <summary>Runs the handler bound to <paramref name="keyInfo"/>, if any. Returns true if handled.</summary>
    public bool Handle(ConsoleKeyInfo keyInfo)
    {
        if (!bindings.TryGetValue(KeyCombo.FromKeyInfo(keyInfo), out var binding))
            return false;
        binding.Handler();
        return true;
    }
}
