namespace ConsoleRender;

/// <summary>A registered global key binding.</summary>
public sealed record KeyBinding(KeyCombo Combo, string Description, Action Handler);
