namespace Wardrobe.Application.Common.Models;

public record ProcessedImageResult(
    byte[] PhotoBytes,
    byte[] ThumbnailBytes
);
