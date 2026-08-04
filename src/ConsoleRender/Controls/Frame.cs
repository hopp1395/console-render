namespace ConsoleRender;

/// <summary>A container with a border and optional title. Children are laid out inside the border.</summary>
public class Frame : Control
{
    private BorderStyle border = BorderStyle.Single;

    public string? Title { get; set; }

    public BorderStyle Border
    {
        get => border;
        set => border = Guard.Against.Null(value);
    }

    public Color BorderColor { get; set; } = Color.Default;
    public Color TitleColor { get; set; } = Color.Default;
    public Color Background { get; set; } = Color.Default;

    public Frame() { }

    public Frame(string title) => Title = Guard.Against.Null(title);

    public override Rect ContentRect => Bounds.Deflate(1);

    protected override Size GetPreferredSize(Size available) => new(available.Width, available.Height);

    protected override void Draw(ConsoleBuffer buffer)
    {
        Guard.Against.Null(buffer);

        if (!Background.IsDefault)
            buffer.FillRect(Bounds, ' ', Color.Default, Background);
        buffer.DrawBorder(Bounds, Border, BorderColor, Background, Title, TitleColor);
    }
}
