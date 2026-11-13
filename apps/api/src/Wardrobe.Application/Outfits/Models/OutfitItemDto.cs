namespace Wardrobe.Application.Outfits.Models;

public record OutfitItemDto(
    Guid ItemId,
    string Name,
    short CategoryId,
    string CategoryCode,
    string CategoryNameUk,
    string? ThumbKey,
    short Position,
    string? PrimaryColorHex
);
