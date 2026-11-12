using Wardrobe.Application.Common.Models;

namespace Wardrobe.Application.Common.Interfaces;

public interface IImageProcessorService
{
    bool ValidateImageSignature(Stream imageStream);
    Task<ProcessedImageResult> ProcessItemPhotoAsync(Stream imageStream, CancellationToken cancellationToken = default);
}
