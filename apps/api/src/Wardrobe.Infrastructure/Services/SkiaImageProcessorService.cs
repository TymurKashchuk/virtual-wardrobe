using SkiaSharp;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Models;

namespace Wardrobe.Infrastructure.Services;

public class SkiaImageProcessorService : IImageProcessorService
{
    private const int MaxPhotoDimension = 1600;
    private const int MaxThumbDimension = 400;
    private const int WebpQuality = 80;

    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffMagic = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebpMagic = [0x57, 0x45, 0x42, 0x50];

    public bool ValidateImageSignature(Stream imageStream)
    {
        if (imageStream == null || !imageStream.CanRead)
        {
            return false;
        }

        var initialPosition = imageStream.CanSeek ? imageStream.Position : 0;
        var header = new byte[12];

        try
        {
            var read = imageStream.Read(header, 0, header.Length);
            if (read < 12)
            {
                return false;
            }

            if (header.Take(3).SequenceEqual(JpegMagic))
            {
                return true;
            }

            if (header.Take(8).SequenceEqual(PngMagic))
            {
                return true;
            }

            if (header.Take(4).SequenceEqual(RiffMagic) && header.Skip(8).Take(4).SequenceEqual(WebpMagic))
            {
                return true;
            }

            return false;
        }
        finally
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = initialPosition;
            }
        }
    }

    public Task<ProcessedImageResult> ProcessItemPhotoAsync(
        Stream imageStream, 
        CancellationToken cancellationToken = default)
    {
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        using var codec = SKCodec.Create(imageStream);
        if (codec == null)
        {
            throw new InvalidOperationException("Failed to decode image stream.");
        }

        var origin = codec.EncodedOrigin;

        using var initialBitmap = SKBitmap.Decode(codec);
        if (initialBitmap == null)
        {
            throw new InvalidOperationException("Failed to decode image bitmap.");
        }

        using var orientedBitmap = ApplyOrientation(initialBitmap, origin);

        var photoBytes = CreateResizedWebp(orientedBitmap, MaxPhotoDimension);
        var thumbBytes = CreateResizedWebp(orientedBitmap, MaxThumbDimension);

        return Task.FromResult(new ProcessedImageResult(photoBytes, thumbBytes));
    }

    private static SKBitmap ApplyOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft || origin == SKEncodedOrigin.Default)
        {
            return bitmap.Copy();
        }

        SKBitmap rotated;
        switch (origin)
        {
            case SKEncodedOrigin.BottomRight: // 180 degrees
                rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                using (var canvas = new SKCanvas(rotated))
                {
                    canvas.RotateDegrees(180, bitmap.Width / 2f, bitmap.Height / 2f);
                    canvas.DrawBitmap(bitmap, 0, 0);
                }
                return rotated;

            case SKEncodedOrigin.RightTop: // 90 degrees clockwise
                rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                using (var canvas = new SKCanvas(rotated))
                {
                    canvas.Translate(bitmap.Height, 0);
                    canvas.RotateDegrees(90);
                    canvas.DrawBitmap(bitmap, 0, 0);
                }
                return rotated;

            case SKEncodedOrigin.LeftBottom: // 270 degrees clockwise (90 counter-clockwise)
                rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                using (var canvas = new SKCanvas(rotated))
                {
                    canvas.Translate(0, bitmap.Width);
                    canvas.RotateDegrees(270);
                    canvas.DrawBitmap(bitmap, 0, 0);
                }
                return rotated;

            default:
                return bitmap.Copy();
        }
    }

    private static byte[] CreateResizedWebp(SKBitmap source, int maxDimension)
    {
        var (targetWidth, targetHeight) = CalculateTargetSize(source.Width, source.Height, maxDimension);

        using var targetBitmap = new SKBitmap(targetWidth, targetHeight, source.ColorType, source.AlphaType);
        using (var canvas = new SKCanvas(targetBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            var destRect = new SKRect(0, 0, targetWidth, targetHeight);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };
            canvas.DrawBitmap(source, destRect, paint);
        }

        using var image = SKImage.FromBitmap(targetBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);

        return data.ToArray();
    }

    private static (int Width, int Height) CalculateTargetSize(int originalWidth, int originalHeight, int maxDimension)
    {
        var longerSide = Math.Max(originalWidth, originalHeight);
        if (longerSide <= maxDimension)
        {
            return (originalWidth, originalHeight);
        }

        var ratio = (double)maxDimension / longerSide;
        var width = (int)Math.Round(originalWidth * ratio);
        var height = (int)Math.Round(originalHeight * ratio);

        return (Math.Max(1, width), Math.Max(1, height));
    }
}
