using Microsoft.EntityFrameworkCore;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Application.Common.Interfaces;

public interface IWardrobeDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Color> Colors { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemColor> ItemColors { get; }
    DbSet<Outfit> Outfits { get; }
    DbSet<OutfitItem> OutfitItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
