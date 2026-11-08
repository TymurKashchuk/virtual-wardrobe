namespace Wardrobe.Domain.Entities;

public class OutfitItem
{
    public Guid OutfitId { get; set; }
    public Outfit? Outfit { get; set; }
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public short Position { get; set; }
}
