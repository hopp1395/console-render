namespace ConsoleRender;

/// <summary>A registered slash command.</summary>
public sealed record CommandDefinition(string Name, string Description, Action<string[]> Handler);
