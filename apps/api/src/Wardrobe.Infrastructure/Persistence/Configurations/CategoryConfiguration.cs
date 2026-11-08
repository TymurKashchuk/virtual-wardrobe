using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.NameUk)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.MaxPerOutfit)
            .IsRequired();

        builder.Property(c => c.SortOrder)
            .IsRequired();

        builder.HasData(
            new Category { Id = 1, Code = "outerwear", NameUk = "Верхній одяг", MaxPerOutfit = 1, SortOrder = 1 },
            new Category { Id = 2, Code = "dress", NameUk = "Сукні", MaxPerOutfit = 1, SortOrder = 2 },
            new Category { Id = 3, Code = "top", NameUk = "Верх", MaxPerOutfit = 1, SortOrder = 3 },
            new Category { Id = 4, Code = "bottom", NameUk = "Низ", MaxPerOutfit = 1, SortOrder = 4 },
            new Category { Id = 5, Code = "shoes", NameUk = "Взуття", MaxPerOutfit = 1, SortOrder = 5 },
            new Category { Id = 6, Code = "accessories", NameUk = "Аксесуари", MaxPerOutfit = 3, SortOrder = 6 }
        );
    }
}
