namespace ConsoleRender;

/// <summary>An integer point in screen coordinates.</summary>
public readonly record struct Point(int X, int Y);

/// <summary>An integer size in character cells.</summary>
public readonly record struct Size(int Width, int Height);

/// <summary>An integer rectangle in screen coordinates. Right/Bottom are exclusive.</summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(int px, int py)
    {
        return px >= X && px < Right && py >= Y && py < Bottom;
    }

    public Rect Deflate(int amount)
    {
        return Deflate(amount, amount);
    }

    public Rect Deflate(int horizontal, int vertical)
    {
        Guard.Against.Negative(horizontal);
        Guard.Against.Negative(vertical);

        return new Rect(X + horizontal, Y + vertical,
            Math.Max(0, Width - 2 * horizontal), Math.Max(0, Height - 2 * vertical));
    }

    public Rect Intersect(Rect other)
    {
        int x1 = Math.Max(X, other.X);
        int y1 = Math.Max(Y, other.Y);
        int x2 = Math.Min(Right, other.Right);
        int y2 = Math.Min(Bottom, other.Bottom);
        return x2 <= x1 || y2 <= y1 ? new Rect(x1, y1, 0, 0) : new Rect(x1, y1, x2 - x1, y2 - y1);
    }
}
