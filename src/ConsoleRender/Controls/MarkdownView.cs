namespace ConsoleRender;

/// <summary>
/// A rendered, read-only preview of Markdown: marker characters are gone, headings bold,
/// emphasis styled, links underlined (their URL hidden), lists bulleted with hanging
/// indents, quotes set behind a gutter, code blocks on their own background and colored by
/// language. Paragraphs are word-wrapped to the view's width.
///
/// When focused: ↑/↓, PageUp/PageDown and Home/End scroll.
/// </summary>
public class MarkdownView : Control
{
    private string markdown = "";
    private Color headingColor = Color.Cyan;
    private Color codeColor = Color.Orange;
    private Color linkTextColor = Color.Blue;
    private Color quoteColor = Color.Gray;
    private Color bulletColor = Color.Yellow;
    private Color codeBackground = Color.Rgb(40, 40, 50);
    private Func<string, ISyntaxHighlighter?>? fenceHighlighter = SyntaxHighlighters.ForFence;

    // The layout is rebuilt only when the text, the width or a styling property changes.
    private IReadOnlyList<StyledLine>? layout;
    private int layoutWidth = -1;
    private int scrollY;

    public MarkdownView()
    {
        Focusable = true;
    }

    public string Markdown
    {
        get => markdown;
        set
        {
            markdown = Guard.Against.Null(value);
            layout = null;
        }
    }

    public Color Foreground { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;

    public Color HeadingColor
    {
        get => headingColor;
        set => Restyle(ref headingColor, value);
    }

    public Color CodeColor
    {
        get => codeColor;
        set => Restyle(ref codeColor, value);
    }

    public Color LinkTextColor
    {
        get => linkTextColor;
        set => Restyle(ref linkTextColor, value);
    }

    public Color QuoteColor
    {
        get => quoteColor;
        set => Restyle(ref quoteColor, value);
    }

    public Color BulletColor
    {
        get => bulletColor;
        set => Restyle(ref bulletColor, value);
    }

    public Color CodeBackground
    {
        get => codeBackground;
        set => Restyle(ref codeBackground, value);
    }

    /// <summary>
    /// Picks the highlighter for a fenced code block from its info string; null colors all
    /// code in <see cref="CodeColor"/>. Defaults to <see cref="SyntaxHighlighters.ForFence"/>.
    /// </summary>
    public Func<string, ISyntaxHighlighter?>? FenceHighlighter
    {
        get => fenceHighlighter;
        set
        {
            fenceHighlighter = value;
            layout = null;
        }
    }

    /// <summary>The first visible row, as of the last frame.</summary>
    public int ScrollOffset => scrollY;

    private void Restyle(ref Color field, Color value)
    {
        field = value;
        layout = null;
    }

    protected override Size GetPreferredSize(Size available)
    {
        return new Size(Math.Min(available.Width, 60), Math.Min(available.Height, 16));
    }

    public override bool OnKey(ConsoleKeyInfo key)
    {
        var page = Math.Max(1, Bounds.Height - 1);
        switch (key.Key)
        {

            case ConsoleKey.UpArrow:
                scrollY--;
                return true;

            case ConsoleKey.DownArrow:
                scrollY++;
                return true;

            case ConsoleKey.PageUp:
                scrollY -= page;
                return true;

            case ConsoleKey.PageDown:
                scrollY += page;
                return true;

            case ConsoleKey.Home:
                scrollY = 0;
                return true;

            case ConsoleKey.End:
                // Draw clamps this to the last page.
                scrollY = int.MaxValue / 2;
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

        buffer.FillRect(Bounds, ' ', Foreground, Background);

        if (layout is null || layoutWidth != Bounds.Width)
        {
            layout = BuildLayout(Bounds.Width);
            layoutWidth = Bounds.Width;
        }

        // Scroll clamping lives here and only here: resizes and new text heal next frame.
        scrollY = Math.Clamp(scrollY, 0, Math.Max(0, layout.Count - Bounds.Height));

        for (var r = 0; r < Bounds.Height && scrollY + r < layout.Count; r++)
        {
            var line = layout[scrollY + r];
            var y = Bounds.Y + r;
            var background = Background;
            if (!line.LineBackground.IsDefault)
            {
                background = line.LineBackground;
                buffer.FillRect(new Rect(Bounds.X, y, Bounds.Width, 1), ' ', Foreground, background);
            }

            SpanPainter.DrawLine(buffer, Bounds.X, y, Bounds.Width, line.Text, line.Spans,
                Foreground, background, 0);
        }
    }

    private IReadOnlyList<StyledLine> BuildLayout(int width)
    {
        var engine = new MarkdownLayout
        {
            HeadingColor = headingColor,
            CodeColor = codeColor,
            LinkTextColor = linkTextColor,
            QuoteColor = quoteColor,
            BulletColor = bulletColor,
            CodeBackground = codeBackground,
            FenceHighlighter = fenceHighlighter,
        };

        return engine.Build(markdown, width);
    }
}
