using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Persistence.Configurations;

public class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("colors");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.NameUk)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Hex)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(c => c.IsNeutral)
            .IsRequired();

        builder.Property(c => c.SortOrder)
            .IsRequired();

        builder.HasData(
            new Color { Id = 1, Code = "black", NameUk = "Чорний", Hex = "#000000", IsNeutral = true, SortOrder = 1 },
            new Color { Id = 2, Code = "white", NameUk = "Білий", Hex = "#FFFFFF", IsNeutral = true, SortOrder = 2 },
            new Color { Id = 3, Code = "grey", NameUk = "Сірий", Hex = "#808080", IsNeutral = true, SortOrder = 3 },
            new Color { Id = 4, Code = "beige", NameUk = "Бежевий", Hex = "#F5F5DC", IsNeutral = true, SortOrder = 4 },
            new Color { Id = 5, Code = "brown", NameUk = "Коричневий", Hex = "#8B4513", IsNeutral = true, SortOrder = 5 },
            new Color { Id = 6, Code = "red", NameUk = "Червоний", Hex = "#E53935", IsNeutral = false, SortOrder = 6 },
            new Color { Id = 7, Code = "burgundy", NameUk = "Бордовий", Hex = "#800020", IsNeutral = false, SortOrder = 7 },
            new Color { Id = 8, Code = "pink", NameUk = "Рожевий", Hex = "#FF69B4", IsNeutral = false, SortOrder = 8 },
            new Color { Id = 9, Code = "orange", NameUk = "Помаранчевий", Hex = "#FF9800", IsNeutral = false, SortOrder = 9 },
            new Color { Id = 10, Code = "yellow", NameUk = "Жовтий", Hex = "#FDD835", IsNeutral = false, SortOrder = 10 },
            new Color { Id = 11, Code = "green", NameUk = "Зелений", Hex = "#4CAF50", IsNeutral = false, SortOrder = 11 },
            new Color { Id = 12, Code = "cyan", NameUk = "Блакитний", Hex = "#00BCD4", IsNeutral = false, SortOrder = 12 },
            new Color { Id = 13, Code = "blue", NameUk = "Синій", Hex = "#1E88E5", IsNeutral = false, SortOrder = 13 },
            new Color { Id = 14, Code = "purple", NameUk = "Фіолетовий", Hex = "#8E24AA", IsNeutral = false, SortOrder = 14 }
        );
    }
}
