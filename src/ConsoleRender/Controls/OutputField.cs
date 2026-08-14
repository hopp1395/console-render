namespace ConsoleRender;

/// <summary>
/// A scrollable, colored multi-line output field (log view) with word wrapping and an
/// optional typewriter animation for appended lines.
///
/// When focused: PageUp/PageDown scroll, Ctrl+End jumps back to the live tail.
/// </summary>
public class OutputField : Control
{
    private readonly record struct Line(string Text, Color Color, TaskLine? Task = null);

    private readonly List<Line> lines = new();
    private readonly Queue<Line> pending = new();
    private Line? revealing;
    private int revealCount;
    private double revealAccumulator;
    private int scrollOffset; // rows scrolled up from the bottom
    private int maxLines = 1000;
    private double typewriterSpeed = 160;
    private double taskClock; // drives the spinners of running task lines

    /// <summary>Maximum number of retained lines; older lines are dropped.</summary>
    public int MaxLines
    {
        get => maxLines;
        set => maxLines = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>When true, appended lines are revealed character by character.</summary>
    public bool Typewriter { get; set; }

    /// <summary>Reveal speed of the typewriter animation in characters per second.</summary>
    public double TypewriterSpeed
    {
        get => typewriterSpeed;
        set => typewriterSpeed = Guard.Against.NegativeOrZero(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;

    public OutputField()
    {
        Focusable = true;
    }

    public void AppendLine(string text, Color? color = null)
    {
        Guard.Against.Null(text);

        var line = new Line(text, color ?? Color.Default);
        if (Typewriter)
        {
            pending.Enqueue(line);
        }
        else
        {
            Commit(line);
        }
    }

    /// <summary>
    /// Appends a live task line: a spinner animates in front of the text while the task
    /// runs, like a status indicator inside the log. The returned handle updates the text
    /// and freezes the line via <see cref="TaskLine.Complete"/> or <see cref="TaskLine.Fail"/>.
    /// Task lines skip the typewriter queue — the spinner itself is the feedback.
    /// </summary>
    public TaskLine BeginTask(string text)
    {
        Guard.Against.Null(text);

        var task = new TaskLine(text);
        Commit(new Line("", default, task));
        return task;
    }

    public void Clear()
    {
        lines.Clear();
        pending.Clear();
        revealing = null;
        scrollOffset = 0;
    }

    private void Commit(Line line)
    {
        lines.Add(line);
        while (lines.Count > MaxLines)
        {
            lines.RemoveAt(0);
        }
    }

    public override void Update(TimeSpan delta)
    {
        Guard.Against.Negative(delta);

        taskClock += delta.TotalSeconds;

        if (revealing is null && pending.Count > 0)
        {
            revealing = pending.Dequeue();
            revealCount = 0;
            revealAccumulator = 0;
        }

        if (revealing is { } current)
        {
            revealAccumulator += delta.TotalSeconds * TypewriterSpeed;
            revealCount = (int)revealAccumulator;
            if (revealCount >= current.Text.Length)
            {
                Commit(current);
                revealing = null;
            }
        }
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        var page = Math.Max(1, Bounds.Height - 1);
        switch (key.Key)
        {
            case ConsoleKey.PageUp:
                scrollOffset += page;
                return true;
            case ConsoleKey.PageDown:
                scrollOffset = Math.Max(0, scrollOffset - page);
                return true;
            case ConsoleKey.End when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                scrollOffset = 0;
                return true;
        }

        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1)
        {
            return;
        }

        if (!Background.IsDefault)
        {
            buffer.FillRect(Bounds, ' ', Foreground, Background);
        }

        // Wrap all lines (including the one currently being revealed) into display rows.
        // Task lines take their current appearance from their handle each frame.
        var rows = new List<(string Text, Color Color)>();
        foreach (var line in lines)
        {
            if (line.Task is { } task)
            {
                var (text, color) = task.Render(taskClock);
                WrapInto(rows, new Line(text, color));
            }
            else
            {
                WrapInto(rows, line);
            }
        }

        if (revealing is { } current)
        {
            WrapInto(rows, new Line(current.Text[..Math.Min(revealCount, current.Text.Length)], current.Color));
        }

        var maxOffset = Math.Max(0, rows.Count - Bounds.Height);
        scrollOffset = Math.Min(scrollOffset, maxOffset);
        var start = Math.Max(0, rows.Count - Bounds.Height - scrollOffset);

        for (var i = 0; i < Bounds.Height && start + i < rows.Count; i++)
        {
            var (text, color) = rows[start + i];
            buffer.Write(Bounds.X, Bounds.Y + i, text,
                color.IsDefault ? Foreground : color, Background);
        }

        if (scrollOffset > 0)
        {
            buffer.Write(Bounds.Right - 3, Bounds.Y, "↑↑↑", Color.Yellow, Background, CellStyle.Bold);
        }
    }

    private void WrapInto(List<(string, Color)> rows, Line line)
    {
        Guard.Against.Null(rows);

        var width = Math.Max(1, Bounds.Width);
        var text = line.Text;
        if (text.Length == 0)
        {
            rows.Add(("", line.Color));
            return;
        }

        // Continuation rows start where the text started, so an indented line keeps its
        // shape instead of falling back to the left edge on every wrap.
        var indent = 0;
        while (indent < text.Length && text[indent] == ' ')
        {
            indent++;
        }

        // Cap the indent at half the field, so a continuation row always carries at least as
        // many characters as padding — and a deeply indented line still terminates.
        indent = Math.Min(indent, width / 2);
        string padding = new(' ', indent);
        var chunk = width - indent;

        rows.Add((text[..Math.Min(width, text.Length)], line.Color));
        for (var pos = width; pos < text.Length; pos += chunk)
        {
            rows.Add((padding + text.Substring(pos, Math.Min(chunk, text.Length - pos)), line.Color));
        }
    }
}
