using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Items.Models;

public record CreateItemRequest(
    string Name,
    short CategoryId,
    short PrimaryColorId,
    IReadOnlyList<short>? SecondaryColorIds,
    ClothingStyle Style,
    string? Notes,
    Stream PhotoStream,
    string PhotoFileName,
    long PhotoLength
);
