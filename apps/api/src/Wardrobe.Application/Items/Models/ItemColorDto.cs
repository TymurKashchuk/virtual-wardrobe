namespace Wardrobe.Application.Items.Models;

public record ItemColorDto(
    short ColorId,
    string Code,
    string NameUk,
    string Hex,
    bool IsNeutral,
    bool IsPrimary
);
