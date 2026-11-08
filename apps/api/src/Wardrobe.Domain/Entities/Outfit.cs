using Wardrobe.Domain.Common;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Domain.Entities;

public class Outfit : IUserOwnedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClothingStyle? Style { get; set; }
    public string? Notes { get; set; }
    public bool IsFavorite { get; set; }
    public OutfitSource Source { get; set; } = OutfitSource.Manual;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<OutfitItem> OutfitItems { get; set; } = new List<OutfitItem>();
}
