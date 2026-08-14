namespace ConsoleRender;

/// <summary>
/// A live log line for a running task, obtained from <see cref="OutputField.BeginTask"/>.
///
/// While <see cref="Running"/>, the line carries an animated spinner — like a status
/// indicator, but inside the log. <see cref="Text"/> may change at any time, so the line
/// can report progress. <see cref="Complete"/> or <see cref="Fail"/> freeze it with a
/// check mark or a cross; <see cref="Finish"/> allows any marker and color.
/// </summary>
public sealed class TaskLine
{
    private static readonly char[] Frames = { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
    private const double FrameInterval = 0.08;

    private string text;
    private char marker = '✓';
    private Color finalColor = Color.Green;

    internal TaskLine(string text)
    {
        this.text = text;
    }

    /// <summary>The message after the spinner or marker. May change while running.</summary>
    public string Text
    {
        get => text;
        set => text = Guard.Against.Null(value);
    }

    /// <summary>True until the task was finished. The spinner animates only while true.</summary>
    public bool Running { get; private set; } = true;

    /// <summary>Line color while the task is running.</summary>
    public Color RunningColor { get; set; } = Color.Cyan;

    /// <summary>Freezes the line with a green check mark, optionally replacing the text.</summary>
    public void Complete(string? text = null)
    {
        Finish('✓', Color.Green, text);
    }

    /// <summary>Freezes the line with a red cross, optionally replacing the text.</summary>
    public void Fail(string? text = null)
    {
        Finish('✗', Color.Red, text);
    }

    /// <summary>Freezes the line with a custom marker and color, optionally replacing the text.</summary>
    public void Finish(char marker, Color color, string? text = null)
    {
        Running = false;
        this.marker = marker;
        finalColor = color;
        if (text is not null)
            this.text = text;
    }

    /// <summary>The line as it should appear right now; the clock drives the spinner.</summary>
    internal (string Text, Color Color) Render(double clock)
    {
        return Running
            ? ($"{Frames[(int)(clock / FrameInterval) % Frames.Length]} {text}", RunningColor)
            : ($"{marker} {text}", finalColor);
    }
}
