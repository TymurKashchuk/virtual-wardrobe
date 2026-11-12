using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Items.Models;

public record ItemDto(
    Guid Id,
    string Name,
    short CategoryId,
    string CategoryCode,
    string CategoryNameUk,
    ClothingStyle Style,
    string? Notes,
    string PhotoKey,
    string ThumbKey,
    IReadOnlyList<ItemColorDto> Colors,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
