namespace Wardrobe.Domain.Entities;

public class Color
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameUk { get; set; } = string.Empty;
    public string Hex { get; set; } = string.Empty;
    public bool IsNeutral { get; set; }
    public short SortOrder { get; set; }

    public ICollection<ItemColor> ItemColors { get; set; } = new List<ItemColor>();
}
