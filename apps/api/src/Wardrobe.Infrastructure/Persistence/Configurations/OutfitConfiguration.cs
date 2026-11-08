using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Persistence.Configurations;

public class OutfitConfiguration : IEntityTypeConfiguration<Outfit>
{
    public void Configure(EntityTypeBuilder<Outfit> builder)
    {
        builder.ToTable("outfits");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(o => o.Style)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Source)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.Property(o => o.IsFavorite)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        builder.HasIndex(o => new { o.UserId, o.IsFavorite });
        builder.HasIndex(o => new { o.UserId, o.CreatedAt });

        builder.HasOne(o => o.User)
            .WithMany(u => u.Outfits)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
