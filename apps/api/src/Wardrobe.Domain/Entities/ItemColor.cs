namespace Wardrobe.Domain.Entities;

public class ItemColor
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public short ColorId { get; set; }
    public Color? Color { get; set; }
    public bool IsPrimary { get; set; }
}
