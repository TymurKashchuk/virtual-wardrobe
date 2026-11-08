using Wardrobe.Domain.Common;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Domain.Entities;

public class Item : IUserOwnedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public short CategoryId { get; set; }
    public Category? Category { get; set; }
    public ClothingStyle Style { get; set; } = ClothingStyle.Casual;
    public string? Notes { get; set; }
    public string PhotoKey { get; set; } = string.Empty;
    public string ThumbKey { get; set; } = string.Empty;
    public string? PhotoCutoutKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ItemColor> ItemColors { get; set; } = new List<ItemColor>();
    public ICollection<OutfitItem> OutfitItems { get; set; } = new List<OutfitItem>();
}
