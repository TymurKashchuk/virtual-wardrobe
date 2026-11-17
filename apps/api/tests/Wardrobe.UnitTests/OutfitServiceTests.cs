using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Outfits.Models;
using Wardrobe.Application.Outfits.Services;
using Wardrobe.Application.Outfits.Validators;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;
using Wardrobe.Infrastructure.Persistence;

namespace Wardrobe.UnitTests;

public class OutfitServiceTests
{
    private static WardrobeDbContext CreateSeededDbContext()
    {
        var options = new DbContextOptionsBuilder<WardrobeDbContext>()
            .UseInMemoryDatabase($"wardrobe_outfit_tests_{Guid.NewGuid():N}")
            .Options;

        var dbContext = new WardrobeDbContext(options, new StubCurrentUserService());
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }

    private static Item CreateItem(Guid userId, short categoryId, string name)
    {
        return new Item
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = categoryId,
            Name = name,
            Style = ClothingStyle.Casual,
            PhotoKey = $"items/{userId}/{Guid.NewGuid():N}.webp",
            ThumbKey = $"items/{userId}/{Guid.NewGuid():N}_thumb.webp"
        };
    }

    [Fact]
    public async Task CreateOutfitAsync_WithValidSlots_PersistsOrderAndReturnsDto()
    {
        await using var dbContext = CreateSeededDbContext();
        var userId = Guid.NewGuid();
        var top = CreateItem(userId, 3, "White Tee");
        var bottom = CreateItem(userId, 4, "Blue Jeans");
        var shoes = CreateItem(userId, 5, "Sneakers");
        dbContext.Items.AddRange(top, bottom, shoes);
        await dbContext.SaveChangesAsync();

        var sut = new OutfitService(dbContext);
        var result = await sut.CreateOutfitAsync(
            userId,
            new CreateOutfitRequest("Casual Day", [top.Id, bottom.Id, shoes.Id], ClothingStyle.Casual));

        Assert.Equal("Casual Day", result.Name);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal([top.Id, bottom.Id, shoes.Id], result.Items.Select(i => i.ItemId).ToArray());
        Assert.Equal([0, 1, 2], result.Items.Select(i => i.Position).ToArray());
    }

    [Fact]
    public async Task CreateOutfitAsync_CombiningDressWithTopOrBottom_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateSeededDbContext();
        var userId = Guid.NewGuid();
        var dress = CreateItem(userId, 2, "Summer Dress");
        var bottom = CreateItem(userId, 4, "Skirt");
        dbContext.Items.AddRange(dress, bottom);
        await dbContext.SaveChangesAsync();

        var sut = new OutfitService(dbContext);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateOutfitAsync(userId, new CreateOutfitRequest("Invalid", [dress.Id, bottom.Id])));

        Assert.Contains("dress", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOutfitAsync_ExceedingMaxAccessoriesOrTops_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateSeededDbContext();
        var userId = Guid.NewGuid();
        var top = CreateItem(userId, 3, "Shirt");
        var acc1 = CreateItem(userId, 6, "Watch");
        var acc2 = CreateItem(userId, 6, "Belt");
        var acc3 = CreateItem(userId, 6, "Bag");
        var acc4 = CreateItem(userId, 6, "Hat");
        dbContext.Items.AddRange(top, acc1, acc2, acc3, acc4);
        await dbContext.SaveChangesAsync();

        var sut = new OutfitService(dbContext);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateOutfitAsync(
                userId,
                new CreateOutfitRequest("Too many acc", [top.Id, acc1.Id, acc2.Id, acc3.Id, acc4.Id])));

        Assert.Contains("accessories", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOutfitAsync_WithAnotherUsersItem_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateSeededDbContext();
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var ownerTop = CreateItem(ownerId, 3, "Owner Shirt");
        var strangerShoes = CreateItem(strangerId, 5, "Stranger Shoes");
        dbContext.Items.AddRange(ownerTop, strangerShoes);
        await dbContext.SaveChangesAsync();

        var sut = new OutfitService(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateOutfitAsync(ownerId, new CreateOutfitRequest("Stolen", [ownerTop.Id, strangerShoes.Id])));
    }

    [Fact]
    public async Task CreateOutfitValidator_RejectsSingleDistinctItem()
    {
        var validator = new CreateOutfitRequestValidator();
        var sameId = Guid.NewGuid();

        var singleResult = await validator.ValidateAsync(new CreateOutfitRequest("One", [sameId]));
        var duplicateResult = await validator.ValidateAsync(new CreateOutfitRequest("Dup", [sameId, sameId]));

        Assert.False(singleResult.IsValid);
        Assert.False(duplicateResult.IsValid);
    }

    [Fact]
    public async Task DuplicateOutfitAsync_CopiesItemsResetsFavoriteAndPrefixesName()
    {
        await using var dbContext = CreateSeededDbContext();
        var userId = Guid.NewGuid();
        var top = CreateItem(userId, 3, "Hoodie");
        var bottom = CreateItem(userId, 4, "Joggers");
        dbContext.Items.AddRange(top, bottom);
        await dbContext.SaveChangesAsync();

        var sut = new OutfitService(dbContext);
        var created = await sut.CreateOutfitAsync(userId, new CreateOutfitRequest("Walk", [top.Id, bottom.Id]));
        await sut.ToggleFavoriteAsync(userId, created.Id);

        var duplicate = await sut.DuplicateOutfitAsync(userId, created.Id);

        Assert.NotEqual(created.Id, duplicate.Id);
        Assert.Equal("Копія Walk", duplicate.Name);
        Assert.False(duplicate.IsFavorite);
        Assert.Equal([top.Id, bottom.Id], duplicate.Items.Select(i => i.ItemId).ToArray());
    }

    [Fact]
    public async Task ToggleFavorite_And_GetOutfitsByItemId_ReturnExpectedResults()
    {
        await using var dbContext = CreateSeededDbContext();
        var userId = Guid.NewGuid();
        var top = CreateItem(userId, 3, "Blouse");
        var bottom = CreateItem(userId, 4, "Trousers");
        var dress = CreateItem(userId, 2, "Evening Dress");
        var shoes = CreateItem(userId, 5, "Heels");
        dbContext.Items.AddRange(top, bottom, dress, shoes);
        await dbContext.SaveChangesAsync();

        var sut = new OutfitService(dbContext);
        var office = await sut.CreateOutfitAsync(userId, new CreateOutfitRequest("Office", [top.Id, bottom.Id, shoes.Id]));
        var party = await sut.CreateOutfitAsync(userId, new CreateOutfitRequest("Party", [dress.Id, shoes.Id]));

        var firstToggle = await sut.ToggleFavoriteAsync(userId, office.Id);
        var secondToggle = await sut.ToggleFavoriteAsync(userId, office.Id);

        Assert.True(firstToggle);
        Assert.False(secondToggle);

        var outfitsWithShoes = await sut.GetOutfitsByItemIdAsync(userId, shoes.Id);
        var outfitsWithTop = await sut.GetOutfitsByItemIdAsync(userId, top.Id);

        Assert.Equal(2, outfitsWithShoes.Count);
        Assert.Single(outfitsWithTop);
        Assert.Equal(office.Id, outfitsWithTop[0].Id);
        Assert.Contains(outfitsWithShoes, o => o.Id == party.Id);
    }
}
