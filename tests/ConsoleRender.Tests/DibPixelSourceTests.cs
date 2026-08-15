using System.Buffers.Binary;

namespace ConsoleRender.Tests;

public class DibPixelSourceTests
{
    /// <summary>
    /// Builds a minimal CF_DIB blob: a BITMAPINFOHEADER followed by the pixel array.
    /// Pixels are supplied as BGR triples in the DIB's own row order.
    /// </summary>
    private static byte[] BuildDib(int width, int height, int bitsPerPixel, bool bottomUp,
        Func<int, int, (byte B, byte G, byte R)> pixel)
    {
        var bytesPerPixel = bitsPerPixel / 8;
        var stride = (width * bytesPerPixel + 3) & ~3;
        var data = new byte[40 + stride * height];
        var span = data.AsSpan();

        BinaryPrimitives.WriteInt32LittleEndian(span, 40);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], width);
        BinaryPrimitives.WriteInt32LittleEndian(span[8..], bottomUp ? height : -height);
        BinaryPrimitives.WriteInt16LittleEndian(span[12..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[14..], (short)bitsPerPixel);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 0); // BI_RGB

        for (var row = 0; row < height; row++)
        {
            for (var x = 0; x < width; x++)
            {
                var (b, g, r) = pixel(x, row);
                var offset = 40 + row * stride + x * bytesPerPixel;
                data[offset] = b;
                data[offset + 1] = g;
                data[offset + 2] = r;
            }
        }

        return data;
    }

    [Fact]
    public void TryParse_ReadsA24BitTopDownImage()
    {
        // Top-down: DIB row 0 is the top row, so it maps straight to y = 0.
        var dib = BuildDib(2, 2, 24, bottomUp: false,
            (x, row) => ((byte)(row * 10), (byte)(x * 20), 100));

        Assert.True(DibPixelSource.TryParse(dib, out var source));
        Assert.Equal(2, source.Width);
        Assert.Equal(2, source.Height);
        Assert.Equal((100, 0, 0), source.GetPixel(0, 0));
        Assert.Equal((100, 20, 0), source.GetPixel(1, 0));
        Assert.Equal((100, 0, 10), source.GetPixel(0, 1));
    }

    [Fact]
    public void TryParse_FlipsBottomUpImages()
    {
        // Bottom-up: DIB row 0 is the bottom row, so it must surface as the last y.
        var dib = BuildDib(1, 2, 24, bottomUp: true,
            (_, row) => (0, 0, (byte)(row == 0 ? 1 : 2)));

        Assert.True(DibPixelSource.TryParse(dib, out var source));
        Assert.Equal((byte)2, source.GetPixel(0, 0).R);
        Assert.Equal((byte)1, source.GetPixel(0, 1).R);
    }

    [Fact]
    public void TryParse_Reads32BitImages()
    {
        var dib = BuildDib(3, 1, 32, bottomUp: false, (x, _) => (1, 2, (byte)(x + 3)));

        Assert.True(DibPixelSource.TryParse(dib, out var source));
        Assert.Equal((5, 2, 1), source.GetPixel(2, 0));
    }

    [Fact]
    public void TryParse_HonoursRowPadding()
    {
        // 3 pixels * 3 bytes = 9 bytes, padded to a stride of 12.
        var dib = BuildDib(3, 2, 24, bottomUp: false, (x, row) => (0, 0, (byte)(row * 3 + x)));

        Assert.True(DibPixelSource.TryParse(dib, out var source));
        Assert.Equal((byte)3, source.GetPixel(0, 1).R);
        Assert.Equal((byte)5, source.GetPixel(2, 1).R);
    }

    [Fact]
    public void TryParse_RejectsTruncatedData()
    {
        var dib = BuildDib(4, 4, 24, bottomUp: false, (_, _) => (0, 0, 0));

        Assert.False(DibPixelSource.TryParse(dib[..(dib.Length - 10)], out _));
    }

    [Fact]
    public void TryParse_RejectsUnsupportedBitDepths()
    {
        var dib = BuildDib(2, 2, 24, bottomUp: false, (_, _) => (0, 0, 0));
        BinaryPrimitives.WriteInt16LittleEndian(dib.AsSpan()[14..], 8);

        Assert.False(DibPixelSource.TryParse(dib, out _));
    }

    [Fact]
    public void TryParse_RejectsTooShortInput()
    {
        Assert.False(DibPixelSource.TryParse(new byte[10], out _));
    }

    [Fact]
    public void GetPixel_RejectsCoordinatesOutsideTheImage()
    {
        var dib = BuildDib(2, 2, 24, bottomUp: false, (_, _) => (0, 0, 0));
        Assert.True(DibPixelSource.TryParse(dib, out var source));

        Assert.Throws<ArgumentOutOfRangeException>(() => source.GetPixel(2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.GetPixel(0, -1));
    }
}
