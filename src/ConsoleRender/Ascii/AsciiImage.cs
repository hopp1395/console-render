namespace ConsoleRender;

/// <summary>A grid of colored glyphs, typically produced from a bitmap by <see cref="AsciiImageConverter"/>.</summary>
public sealed class AsciiImage
{
    private readonly (char Char, Color Color)[] cells;

    public int Width { get; }
    public int Height { get; }

    public AsciiImage(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        Width = width;
        Height = height;
        cells = new (char, Color)[width * height];
    }

    public (char Char, Color Color) this[int x, int y]
    {
        get => cells[y * Width + x];
        set => cells[y * Width + x] = value;
    }
}
