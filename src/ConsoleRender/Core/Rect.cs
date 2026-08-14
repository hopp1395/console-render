namespace ConsoleRender;

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
        var x1 = Math.Max(X, other.X);
        var y1 = Math.Max(Y, other.Y);
        var x2 = Math.Min(Right, other.Right);
        var y2 = Math.Min(Bottom, other.Bottom);
        return x2 <= x1 || y2 <= y1 ? new Rect(x1, y1, 0, 0) : new Rect(x1, y1, x2 - x1, y2 - y1);
    }
}
