using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Items.Models;

public record UpdateItemRequest(
    string Name,
    short CategoryId,
    short PrimaryColorId,
    IReadOnlyList<short>? SecondaryColorIds,
    ClothingStyle Style,
    string? Notes,
    Stream? NewPhotoStream = null,
    string? NewPhotoFileName = null,
    long? NewPhotoLength = null
);
