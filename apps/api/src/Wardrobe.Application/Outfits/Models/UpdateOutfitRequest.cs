using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Outfits.Models;

public record UpdateOutfitRequest(
    string Name,
    IReadOnlyList<Guid> ItemIds,
    ClothingStyle? Style = null,
    string? Notes = null
);
