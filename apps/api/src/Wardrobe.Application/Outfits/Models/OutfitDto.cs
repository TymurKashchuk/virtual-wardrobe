using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Outfits.Models;

public record OutfitDto(
    Guid Id,
    string Name,
    ClothingStyle? Style,
    string? Notes,
    bool IsFavorite,
    OutfitSource Source,
    IReadOnlyList<OutfitItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
