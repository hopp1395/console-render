namespace ConsoleRender;

/// <summary>Source of RGB pixels for ASCII conversion.</summary>
public interface IPixelSource
{
    int Width { get; }
    int Height { get; }
    (byte R, byte G, byte B) GetPixel(int x, int y);
}
