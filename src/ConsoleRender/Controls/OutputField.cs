namespace ConsoleRender;

/// <summary>
/// A scrollable, colored multi-line output field (log view) with word wrapping and an
/// optional typewriter animation for appended lines.
///
/// When focused: PageUp/PageDown scroll, Ctrl+End jumps back to the live tail.
/// </summary>
public class OutputField : Control
{
    private readonly record struct Line(string Text, Color Color);

    private readonly List<Line> _lines = new();
    private readonly Queue<Line> _pending = new();
    private Line? _revealing;
    private int _revealCount;
    private double _revealAccumulator;
    private int _scrollOffset; // rows scrolled up from the bottom
    private int _maxLines = 1000;
    private double _typewriterSpeed = 160;

    /// <summary>Maximum number of retained lines; older lines are dropped.</summary>
    public int MaxLines
    {
        get => _maxLines;
        set => _maxLines = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>When true, appended lines are revealed character by character.</summary>
    public bool Typewriter { get; set; }

    /// <summary>Reveal speed of the typewriter animation in characters per second.</summary>
    public double TypewriterSpeed
    {
        get => _typewriterSpeed;
        set => _typewriterSpeed = Guard.Against.NegativeOrZero(value);
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;

    public OutputField() => Focusable = true;

    public void AppendLine(string text, Color? color = null)
    {
        Guard.Against.Null(text);

        var line = new Line(text, color ?? Color.Default);
        if (Typewriter)
            _pending.Enqueue(line);
        else
            Commit(line);
    }

    public void Clear()
    {
        _lines.Clear();
        _pending.Clear();
        _revealing = null;
        _scrollOffset = 0;
    }

    private void Commit(Line line)
    {
        _lines.Add(line);
        while (_lines.Count > MaxLines)
            _lines.RemoveAt(0);
    }

    public override void Update(TimeSpan delta)
    {
        Guard.Against.Negative(delta);

        if (_revealing is null && _pending.Count > 0)
        {
            _revealing = _pending.Dequeue();
            _revealCount = 0;
            _revealAccumulator = 0;
        }

        if (_revealing is { } current)
        {
            _revealAccumulator += delta.TotalSeconds * TypewriterSpeed;
            _revealCount = (int)_revealAccumulator;
            if (_revealCount >= current.Text.Length)
            {
                Commit(current);
                _revealing = null;
            }
        }
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        int page = Math.Max(1, Bounds.Height - 1);
        switch (key.Key)
        {
            case ConsoleKey.PageUp:
                _scrollOffset += page;
                return true;
            case ConsoleKey.PageDown:
                _scrollOffset = Math.Max(0, _scrollOffset - page);
                return true;
            case ConsoleKey.End when key.Modifiers.HasFlag(ConsoleModifiers.Control):
                _scrollOffset = 0;
                return true;
        }
        return false;
    }

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (Bounds.Width < 1 || Bounds.Height < 1) return;

        if (!Background.IsDefault)
            buffer.FillRect(Bounds, ' ', Foreground, Background);

        // Wrap all lines (including the one currently being revealed) into display rows.
        var rows = new List<(string Text, Color Color)>();
        foreach (var line in _lines)
            WrapInto(rows, line);
        if (_revealing is { } current)
            WrapInto(rows, new Line(current.Text[..Math.Min(_revealCount, current.Text.Length)], current.Color));

        int maxOffset = Math.Max(0, rows.Count - Bounds.Height);
        _scrollOffset = Math.Min(_scrollOffset, maxOffset);
        int start = Math.Max(0, rows.Count - Bounds.Height - _scrollOffset);

        for (int i = 0; i < Bounds.Height && start + i < rows.Count; i++)
        {
            var (text, color) = rows[start + i];
            buffer.Write(Bounds.X, Bounds.Y + i, text,
                color.IsDefault ? Foreground : color, Background);
        }

        if (_scrollOffset > 0)
            buffer.Write(Bounds.Right - 3, Bounds.Y, "↑↑↑", Color.Yellow, Background, CellStyle.Bold);
    }

    private void WrapInto(List<(string, Color)> rows, Line line)
    {
        Guard.Against.Null(rows);

        int width = Math.Max(1, Bounds.Width);
        string text = line.Text;
        if (text.Length == 0)
        {
            rows.Add(("", line.Color));
            return;
        }

        // Continuation rows start where the text started, so an indented line keeps its
        // shape instead of falling back to the left edge on every wrap.
        int indent = 0;
        while (indent < text.Length && text[indent] == ' ')
            indent++;
        // Cap the indent at half the field, so a continuation row always carries at least as
        // many characters as padding — and a deeply indented line still terminates.
        indent = Math.Min(indent, width / 2);
        string padding = new(' ', indent);
        int chunk = width - indent;

        rows.Add((text[..Math.Min(width, text.Length)], line.Color));
        for (int pos = width; pos < text.Length; pos += chunk)
            rows.Add((padding + text.Substring(pos, Math.Min(chunk, text.Length - pos)), line.Color));
    }
}
