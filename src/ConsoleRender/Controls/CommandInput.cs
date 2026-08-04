namespace ConsoleRender;

/// <summary>
/// A <see cref="TextBox"/> that executes "/command" input through a <see cref="CommandRegistry"/>.
/// Plain text (without leading slash) is raised via <see cref="TextBox.Submitted"/>.
/// Tab completes command names while the input starts with '/'.
/// </summary>
public class CommandInput : TextBox
{
    public CommandRegistry Commands { get; } = new();

    /// <summary>Raised after a command was executed (successfully or not).</summary>
    public event Action<CommandResult>? CommandExecuted;

    public CommandInput()
    {
        Placeholder = "Type text or /command …";
    }

    protected override void OnSubmit(string text)
    {
        Guard.Against.Null(text);

        string trimmed = text.Trim();
        SetText("");

        if (trimmed.Length == 0)
            return;

        if (trimmed.StartsWith('/'))
        {
            var result = Commands.Execute(trimmed);
            CommandExecuted?.Invoke(result);
        }
        else
        {
            RaiseSubmitted(trimmed);
        }
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        // Tab completion only while typing a command; otherwise Tab keeps cycling focus.
        if (key.Key == ConsoleKey.Tab && Text.StartsWith('/'))
        {
            string prefix = Text[1..];
            if (!prefix.Contains(' '))
            {
                var matches = Commands.Complete(prefix);
                if (matches.Count == 1)
                    SetText("/" + matches[0] + " ");
                else if (matches.Count > 1)
                    SetText("/" + CommonPrefix(matches));
            }
            return true;
        }
        return base.OnKey(key);
    }

    private static string CommonPrefix(IReadOnlyList<string> values)
    {
        Guard.Against.NullOrEmpty(values);

        string prefix = values[0];
        foreach (string v in values.Skip(1))
        {
            int len = 0;
            while (len < prefix.Length && len < v.Length &&
                   char.ToLowerInvariant(prefix[len]) == char.ToLowerInvariant(v[len]))
                len++;
            prefix = prefix[..len];
        }
        return prefix;
    }
}
