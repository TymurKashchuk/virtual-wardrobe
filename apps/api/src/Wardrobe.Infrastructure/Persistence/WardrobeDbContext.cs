using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Persistence;

public class WardrobeDbContext : DbContext, IWardrobeDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public WardrobeDbContext(
        DbContextOptions<WardrobeDbContext> options,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemColor> ItemColors => Set<ItemColor>();
    public DbSet<Outfit> Outfits => Set<Outfit>();
    public DbSet<OutfitItem> OutfitItems => Set<OutfitItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WardrobeDbContext).Assembly);

        modelBuilder.Entity<Item>().HasQueryFilter(i =>
            _currentUserService == null ||
            !_currentUserService.IsAuthenticated ||
            i.UserId == _currentUserService.UserId);

        modelBuilder.Entity<ItemColor>().HasQueryFilter(ic =>
            _currentUserService == null ||
            !_currentUserService.IsAuthenticated ||
            ic.Item!.UserId == _currentUserService.UserId);

        modelBuilder.Entity<Outfit>().HasQueryFilter(o =>
            _currentUserService == null ||
            !_currentUserService.IsAuthenticated ||
            o.UserId == _currentUserService.UserId);

        modelBuilder.Entity<OutfitItem>().HasQueryFilter(oi =>
            _currentUserService == null ||
            !_currentUserService.IsAuthenticated ||
            oi.Outfit!.UserId == _currentUserService.UserId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is Item item)
                {
                    item.UpdatedAt = now;
                }
                else if (entry.Entity is Outfit outfit)
                {
                    outfit.UpdatedAt = now;
                }
                else if (entry.Entity is User user)
                {
                    user.UpdatedAt = now;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
