using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Outfits.Models;

public record OutfitFilterQuery(
    string? Search = null,
    bool? IsFavorite = null,
    ClothingStyle? Style = null,
    int Page = 1,
    int PageSize = 24
);
