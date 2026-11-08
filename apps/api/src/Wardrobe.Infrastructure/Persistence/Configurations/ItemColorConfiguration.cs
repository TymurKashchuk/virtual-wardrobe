using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Persistence.Configurations;

public class ItemColorConfiguration : IEntityTypeConfiguration<ItemColor>
{
    public void Configure(EntityTypeBuilder<ItemColor> builder)
    {
        builder.ToTable("item_colors");

        builder.HasKey(ic => new { ic.ItemId, ic.ColorId });

        builder.Property(ic => ic.IsPrimary)
            .IsRequired();

        builder.HasOne(ic => ic.Item)
            .WithMany(i => i.ItemColors)
            .HasForeignKey(ic => ic.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ic => ic.Color)
            .WithMany(c => c.ItemColors)
            .HasForeignKey(ic => ic.ColorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
