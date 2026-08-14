namespace ConsoleRender;

/// <summary>
/// Registry for "/name arg1 arg2"-style commands. Arguments may be quoted with double quotes.
/// </summary>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, CommandDefinition> commands = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<CommandDefinition> All => commands.Values.OrderBy(c => c.Name);

    public void Register(string name, string description, Action<string[]> handler)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(description);
        Guard.Against.Null(handler);

        var key = name.TrimStart('/');
        Guard.Against.NullOrWhiteSpace(key, nameof(name));

        commands[key] = new CommandDefinition(key, description, handler);
    }

    public bool TryGet(string name, out CommandDefinition command)
    {
        Guard.Against.Null(name);

        return commands.TryGetValue(name.TrimStart('/'), out command!);
    }

    /// <summary>Finds command names starting with <paramref name="prefix"/> (for completion).</summary>
    public IReadOnlyList<string> Complete(string prefix)
    {
        Guard.Against.Null(prefix);

        prefix = prefix.TrimStart('/');
        return commands.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k)
            .ToList();
    }

    /// <summary>Parses and executes a command line like "/echo hello world".</summary>
    public CommandResult Execute(string input)
    {
        Guard.Against.Null(input);

        var tokens = Tokenize(input.TrimStart('/'));
        if (tokens.Length == 0)
        {
            return new CommandResult(false, "Empty command.");
        }

        if (!commands.TryGetValue(tokens[0], out var command))
        {
            return new CommandResult(false, $"Unknown command: /{tokens[0]} (try /help)");
        }

        try
        {
            command.Handler(tokens[1..]);
            return new CommandResult(true, "");
        }
        catch (Exception ex)
        {
            return new CommandResult(false, $"/{command.Name} failed: {ex.Message}");
        }
    }

    /// <summary>Splits a command line into tokens; double quotes group words together.</summary>
    public static string[] Tokenize(string input)
    {
        Guard.Against.Null(input);

        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }
}
