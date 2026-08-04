using System.Buffers.Binary;

namespace ConsoleRender;

/// <summary>
/// Reads a device-independent bitmap (CF_DIB clipboard format) as a pixel source.
/// Supports 24 and 32 bits per pixel, both bottom-up and top-down row order.
/// </summary>
public sealed class DibPixelSource : IPixelSource
{
    private readonly byte[] _data;
    private readonly int _pixelOffset;
    private readonly int _stride;
    private readonly int _bitsPerPixel;
    private readonly bool _bottomUp;

    public int Width { get; }
    public int Height { get; }

    private DibPixelSource(byte[] data, int pixelOffset, int width, int height,
        int bitsPerPixel, int stride, bool bottomUp)
    {
        _data = data;
        _pixelOffset = pixelOffset;
        _bitsPerPixel = bitsPerPixel;
        _stride = stride;
        _bottomUp = bottomUp;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Parses raw CF_DIB bytes (BITMAPINFOHEADER followed by an optional color mask/table
    /// and the pixel array). Returns false if the format is unsupported or the data truncated.
    /// </summary>
    public static bool TryParse(byte[] data, out IPixelSource source)
    {
        Guard.Against.Null(data);

        source = null!;
        if (data.Length < 40) return false;

        var span = data.AsSpan();
        int headerSize = BinaryPrimitives.ReadInt32LittleEndian(span);
        if (headerSize < 40 || headerSize > data.Length) return false;

        int width = BinaryPrimitives.ReadInt32LittleEndian(span[4..]);
        int rawHeight = BinaryPrimitives.ReadInt32LittleEndian(span[8..]);
        int bitCount = BinaryPrimitives.ReadInt16LittleEndian(span[14..]);
        int compression = BinaryPrimitives.ReadInt32LittleEndian(span[16..]);
        int clrUsed = BinaryPrimitives.ReadInt32LittleEndian(span[32..]);

        if (width <= 0 || rawHeight == 0) return false;
        if (bitCount != 24 && bitCount != 32) return false;

        // BI_RGB (0) and BI_BITFIELDS (3) only; BI_BITFIELDS on 32bpp is BGRA in practice.
        if (compression != 0 && compression != 3) return false;

        bool bottomUp = rawHeight > 0;
        int height = Math.Abs(rawHeight);

        // The pixel array starts after the header, the optional bitfield masks and the color table.
        int offset = headerSize;
        if (compression == 3 && headerSize == 40)
            offset += 12; // three DWORD masks follow a plain BITMAPINFOHEADER
        if (bitCount <= 8)
            offset += (clrUsed == 0 ? 1 << bitCount : clrUsed) * 4;

        int stride = (width * bitCount / 8 + 3) & ~3;
        long required = (long)offset + (long)stride * height;
        if (required > data.Length) return false;

        source = new DibPixelSource(data, offset, width, height, bitCount, stride, bottomUp);
        return true;
    }

    public (byte R, byte G, byte B) GetPixel(int x, int y)
    {
        Guard.Against.OutOfRange(x, nameof(x), 0, Width - 1);
        Guard.Against.OutOfRange(y, nameof(y), 0, Height - 1);

        int row = _bottomUp ? Height - 1 - y : y;
        int index = _pixelOffset + row * _stride + x * (_bitsPerPixel / 8);
        // DIB pixel order is BGR(A).
        return (_data[index + 2], _data[index + 1], _data[index]);
    }
}
