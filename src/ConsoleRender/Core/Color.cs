namespace ConsoleRender;

/// <summary>
/// A 24-bit RGB color. The default value represents the terminal's default color.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    private readonly byte _r, _g, _b;
    private readonly bool _hasValue;

    private Color(byte r, byte g, byte b)
    {
        _r = r; _g = g; _b = b;
        _hasValue = true;
    }

    public byte R => _r;
    public byte G => _g;
    public byte B => _b;

    /// <summary>True if this is the terminal's default color rather than an explicit RGB value.</summary>
    public bool IsDefault => !_hasValue;

    /// <summary>The terminal's default foreground/background color.</summary>
    public static readonly Color Default = default;

    public static Color Rgb(byte r, byte g, byte b) => new(r, g, b);

    public static readonly Color Black = Rgb(0, 0, 0);
    public static readonly Color White = Rgb(255, 255, 255);
    public static readonly Color Red = Rgb(224, 82, 82);
    public static readonly Color Green = Rgb(94, 200, 122);
    public static readonly Color Blue = Rgb(84, 144, 235);
    public static readonly Color Yellow = Rgb(229, 192, 84);
    public static readonly Color Cyan = Rgb(83, 197, 208);
    public static readonly Color Magenta = Rgb(199, 104, 219);
    public static readonly Color Orange = Rgb(235, 148, 74);
    public static readonly Color Gray = Rgb(160, 160, 160);
    public static readonly Color DarkGray = Rgb(96, 96, 96);
    public static readonly Color DarkBlue = Rgb(32, 46, 74);

    /// <summary>Creates a color from HSV. Hue in degrees [0..360), saturation and value in [0..1].</summary>
    public static Color FromHsv(double hue, double saturation, double value)
    {
        Guard.Against.OutOfRange(saturation, nameof(saturation), 0, 1);
        Guard.Against.OutOfRange(value, nameof(value), 0, 1);

        hue = ((hue % 360) + 360) % 360;
        double c = value * saturation;
        double x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        double m = value - c;
        (double r, double g, double b) = ((int)(hue / 60)) switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return Rgb((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    /// <summary>Linear interpolation between two colors. Default colors are treated as black.</summary>
    public static Color Lerp(Color a, Color b, double t)
    {
        Guard.Against.OutOfRange(t, nameof(t), 0, 1);

        return Rgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }

    public Color Scale(double factor)
    {
        Guard.Against.OutOfRange(factor, nameof(factor), 0, 1);

        return IsDefault ? this : Rgb((byte)(R * factor), (byte)(G * factor), (byte)(B * factor));
    }

    public bool Equals(Color other) =>
        _hasValue == other._hasValue && _r == other._r && _g == other._g && _b == other._b;

    public override bool Equals(object? obj) => obj is Color c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(_hasValue, _r, _g, _b);
    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    public override string ToString() => IsDefault ? "Default" : $"#{R:X2}{G:X2}{B:X2}";
}
