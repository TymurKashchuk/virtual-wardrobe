namespace Wardrobe.Domain.Entities;

public class Category
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameUk { get; set; } = string.Empty;
    public short MaxPerOutfit { get; set; } = 1;
    public short SortOrder { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
