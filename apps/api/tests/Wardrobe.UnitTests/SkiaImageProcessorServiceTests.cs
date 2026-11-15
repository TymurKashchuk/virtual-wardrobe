using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using SkiaSharp;
using Wardrobe.Infrastructure.Services;

namespace Wardrobe.UnitTests;

public class SkiaImageProcessorServiceTests
{
    private readonly SkiaImageProcessorService _sut = new();

    [Fact]
    public void ValidateImageSignature_RejectsTextDisguisedAsImage()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not actually a jpeg image payload"));

        Assert.False(_sut.ValidateImageSignature(stream));
    }

    [Fact]
    public void ValidateImageSignature_RejectsTooShortStream()
    {
        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        Assert.False(_sut.ValidateImageSignature(stream));
    }

    [Fact]
    public void ValidateImageSignature_AcceptsValidPngAndPreservesStreamPosition()
    {
        using var stream = CreateValidPngStream(10, 10);
        stream.Position = 0;

        var isValid = _sut.ValidateImageSignature(stream);

        Assert.True(isValid);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public async Task ProcessItemPhotoAsync_EncodesValidPngToResizedWebp()
    {
        using var stream = CreateValidPngStream(2000, 1000);

        var result = await _sut.ProcessItemPhotoAsync(stream);

        using var photoBitmap = SKBitmap.Decode(result.PhotoBytes);
        using var thumbBitmap = SKBitmap.Decode(result.ThumbnailBytes);

        Assert.NotNull(photoBitmap);
        Assert.NotNull(thumbBitmap);
        Assert.Equal(1600, photoBitmap.Width);
        Assert.Equal(800, photoBitmap.Height);
        Assert.Equal(400, thumbBitmap.Width);
        Assert.Equal(200, thumbBitmap.Height);
    }

    [Fact]
    public async Task ProcessItemPhotoAsync_RejectsPixelBombDimensionsBeforeDecoding()
    {
        using var bombStream = CreateMinimalPngHeaderWithDimensions(12000, 12000);

        Assert.True(_sut.ValidateImageSignature(bombStream));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ProcessItemPhotoAsync(bombStream));

        Assert.Contains("exceed allowed limits", ex.Message);
    }

    private static MemoryStream CreateValidPngStream(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.CornflowerBlue);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    private static MemoryStream CreateMinimalPngHeaderWithDimensions(int width, int height)
    {
        var ms = new MemoryStream();
        // PNG signature
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk
        var ihdrData = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(4, 4), height);
        ihdrData[8] = 8; // bit depth
        ihdrData[9] = 2; // color type RGB
        WritePngChunk(ms, "IHDR", ihdrData);

        // Minimal zlib stream inside IDAT + IEND so codecs treat it as structurally valid
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write([0, 0, 0, 0]);
        }
        WritePngChunk(ms, "IDAT", compressed.ToArray());
        WritePngChunk(ms, "IEND", []);

        ms.Position = 0;
        return ms;
    }

    private static void WritePngChunk(Stream output, string type, byte[] data)
    {
        var lengthBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBytes, data.Length);
        output.Write(lengthBytes);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        var crcInput = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, typeBytes.Length);
        Buffer.BlockCopy(data, 0, crcInput, typeBytes.Length, data.Length);

        var crcBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, ComputeCrc32(crcInput));
        output.Write(crcBytes);
    }

    private static uint ComputeCrc32(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
            }
        }
        return ~crc;
    }
}
