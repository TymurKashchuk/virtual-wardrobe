using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Outfits.Models;

public record OutfitSummaryDto(
    Guid Id,
    string Name,
    ClothingStyle? Style,
    bool IsFavorite,
    OutfitSource Source,
    int ItemsCount,
    IReadOnlyList<string> ItemThumbKeys,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
