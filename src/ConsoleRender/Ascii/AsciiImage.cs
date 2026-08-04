namespace ConsoleRender;

/// <summary>A grid of colored glyphs, typically produced from a bitmap by <see cref="AsciiImageConverter"/>.</summary>
public sealed class AsciiImage
{
    private readonly (char Char, Color Color)[] _cells;

    public int Width { get; }
    public int Height { get; }

    public AsciiImage(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        Width = width;
        Height = height;
        _cells = new (char, Color)[width * height];
    }

    public (char Char, Color Color) this[int x, int y]
    {
        get => _cells[y * Width + x];
        set => _cells[y * Width + x] = value;
    }
}

/// <summary>Source of RGB pixels for ASCII conversion.</summary>
public interface IPixelSource
{
    int Width { get; }
    int Height { get; }
    (byte R, byte G, byte B) GetPixel(int x, int y);
}

/// <summary>Converts pixel images into colored ASCII art.</summary>
public static class AsciiImageConverter
{
    private const string Ramp = " .:-=+*#%@";

    /// <summary>
    /// Downsamples <paramref name="source"/> to <paramref name="targetWidth"/> columns.
    /// Cell aspect ratio (~1:2) is compensated so proportions look right in the terminal.
    /// </summary>
    public static AsciiImage Convert(IPixelSource source, int targetWidth)
    {
        Guard.Against.Null(source);
        Guard.Against.NegativeOrZero(targetWidth);
        Guard.Against.NegativeOrZero(source.Width, $"{nameof(source)}.{nameof(source.Width)}");
        Guard.Against.NegativeOrZero(source.Height, $"{nameof(source)}.{nameof(source.Height)}");

        int targetHeight = Math.Max(1, (int)Math.Round(
            (double)source.Height / source.Width * targetWidth * 0.5));

        var result = new AsciiImage(targetWidth, targetHeight);

        for (int ty = 0; ty < targetHeight; ty++)
        {
            for (int tx = 0; tx < targetWidth; tx++)
            {
                int x0 = tx * source.Width / targetWidth;
                int x1 = Math.Max(x0 + 1, (tx + 1) * source.Width / targetWidth);
                int y0 = ty * source.Height / targetHeight;
                int y1 = Math.Max(y0 + 1, (ty + 1) * source.Height / targetHeight);

                long r = 0, g = 0, b = 0, count = 0;
                for (int y = y0; y < y1 && y < source.Height; y++)
                    for (int x = x0; x < x1 && x < source.Width; x++)
                    {
                        var (pr, pg, pb) = source.GetPixel(x, y);
                        r += pr; g += pg; b += pb;
                        count++;
                    }
                if (count == 0) count = 1;

                byte ar = (byte)(r / count), ag = (byte)(g / count), ab = (byte)(b / count);
                double brightness = (0.299 * ar + 0.587 * ag + 0.114 * ab) / 255.0;
                char glyph = Ramp[Math.Min(Ramp.Length - 1, (int)(brightness * Ramp.Length))];
                result[tx, ty] = (glyph, Color.Rgb(ar, ag, ab));
            }
        }

        return result;
    }
}
