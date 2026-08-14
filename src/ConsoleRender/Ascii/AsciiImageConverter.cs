namespace ConsoleRender;

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

        var targetHeight = Math.Max(1, (int)Math.Round(
            (double)source.Height / source.Width * targetWidth * 0.5));

        var result = new AsciiImage(targetWidth, targetHeight);

        for (var ty = 0; ty < targetHeight; ty++)
        {
            for (var tx = 0; tx < targetWidth; tx++)
            {
                var x0 = tx * source.Width / targetWidth;
                var x1 = Math.Max(x0 + 1, (tx + 1) * source.Width / targetWidth);
                var y0 = ty * source.Height / targetHeight;
                var y1 = Math.Max(y0 + 1, (ty + 1) * source.Height / targetHeight);

                var r = 0L;
                var g = 0L;
                var b = 0L;
                var count = 0L;
                for (var y = y0; y < y1 && y < source.Height; y++)
                {
                    for (var x = x0; x < x1 && x < source.Width; x++)
                    {
                        var (pr, pg, pb) = source.GetPixel(x, y);
                        r += pr; g += pg; b += pb;
                        count++;
                    }
                }

                if (count == 0)
                {
                    count = 1;
                }

                var ar = (byte)(r / count);
                var ag = (byte)(g / count);
                var ab = (byte)(b / count);
                var brightness = (0.299 * ar + 0.587 * ag + 0.114 * ab) / 255.0;
                var glyph = Ramp[Math.Min(Ramp.Length - 1, (int)(brightness * Ramp.Length))];
                result[tx, ty] = (glyph, Color.Rgb(ar, ag, ab));
            }
        }

        return result;
    }
}
