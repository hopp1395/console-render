namespace ConsoleRender;

/// <summary>Character set used to draw box borders.</summary>
public sealed class BorderStyle
{
    public char TopLeft { get; init; }
    public char TopRight { get; init; }
    public char BottomLeft { get; init; }
    public char BottomRight { get; init; }
    public char Horizontal { get; init; }
    public char Vertical { get; init; }

    public static readonly BorderStyle Single = new()
    {
        TopLeft = '┌', TopRight = '┐', BottomLeft = '└', BottomRight = '┘',
        Horizontal = '─', Vertical = '│',
    };

    public static readonly BorderStyle Double = new()
    {
        TopLeft = '╔', TopRight = '╗', BottomLeft = '╚', BottomRight = '╝',
        Horizontal = '═', Vertical = '║',
    };

    public static readonly BorderStyle Rounded = new()
    {
        TopLeft = '╭', TopRight = '╮', BottomLeft = '╰', BottomRight = '╯',
        Horizontal = '─', Vertical = '│',
    };

    public static readonly BorderStyle Thick = new()
    {
        TopLeft = '┏', TopRight = '┓', BottomLeft = '┗', BottomRight = '┛',
        Horizontal = '━', Vertical = '┃',
    };

    public static readonly BorderStyle Ascii = new()
    {
        TopLeft = '+', TopRight = '+', BottomLeft = '+', BottomRight = '+',
        Horizontal = '-', Vertical = '|',
    };
}
