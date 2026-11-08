using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Persistence.Configurations;

public class OutfitItemConfiguration : IEntityTypeConfiguration<OutfitItem>
{
    public void Configure(EntityTypeBuilder<OutfitItem> builder)
    {
        builder.ToTable("outfit_items");

        builder.HasKey(oi => new { oi.OutfitId, oi.ItemId });

        builder.Property(oi => oi.Position)
            .IsRequired();

        builder.HasOne(oi => oi.Outfit)
            .WithMany(o => o.OutfitItems)
            .HasForeignKey(oi => oi.OutfitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Item)
            .WithMany(i => i.OutfitItems)
            .HasForeignKey(oi => oi.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
