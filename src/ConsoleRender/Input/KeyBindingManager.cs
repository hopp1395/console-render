namespace ConsoleRender;

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
        {
            return false;
        }

        binding.Handler();
        return true;
    }
}
