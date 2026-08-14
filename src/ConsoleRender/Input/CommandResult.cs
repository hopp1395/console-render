namespace ConsoleRender;

/// <summary>Result of executing a command line via <see cref="CommandRegistry.Execute"/>.</summary>
public readonly record struct CommandResult(bool Success, string Message);
