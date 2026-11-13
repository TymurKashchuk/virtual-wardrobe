using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Outfits.Models;

public record CreateOutfitRequest(
    string Name,
    IReadOnlyList<Guid> ItemIds,
    ClothingStyle? Style = null,
    string? Notes = null,
    OutfitSource Source = OutfitSource.Manual
);
