using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Models;
using Wardrobe.Application.Items.Services;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;
using Wardrobe.Infrastructure.Persistence;

namespace Wardrobe.UnitTests;

public class ItemServiceSummaryTests
{
    private static WardrobeDbContext CreateSeededDbContext()
    {
        var options = new DbContextOptionsBuilder<WardrobeDbContext>()
            .UseInMemoryDatabase($"wardrobe_summary_tests_{Guid.NewGuid():N}")
            .Options;

        var dbContext = new WardrobeDbContext(options, new StubCurrentUserService());
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyWardrobe_ReturnsAllSixCategoriesWithZeroCounts()
    {
        await using var dbContext = CreateSeededDbContext();
        var sut = new ItemService(dbContext, new NoOpImageProcessor(), new NoOpFileStorage());

        var summary = await sut.GetSummaryAsync(Guid.NewGuid());

        Assert.Equal(0, summary.TotalCount);
        Assert.Equal(300, summary.MaxItemsLimit);
        Assert.Equal(6, summary.Categories.Count);
        Assert.All(summary.Categories, c => Assert.Equal(0, c.Count));
    }

    [Fact]
    public async Task GetSummaryAsync_CountsOnlyUsersItemsPerCategory()
    {
        await using var dbContext = CreateSeededDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        dbContext.Items.AddRange(
            new Item { UserId = userId, CategoryId = 3, Name = "Tee 1", Style = ClothingStyle.Casual, PhotoKey = "p1", ThumbKey = "t1" },
            new Item { UserId = userId, CategoryId = 3, Name = "Tee 2", Style = ClothingStyle.Casual, PhotoKey = "p2", ThumbKey = "t2" },
            new Item { UserId = userId, CategoryId = 5, Name = "Shoes", Style = ClothingStyle.Sport, PhotoKey = "p3", ThumbKey = "t3" },
            new Item { UserId = otherUserId, CategoryId = 3, Name = "Other Tee", Style = ClothingStyle.Casual, PhotoKey = "p4", ThumbKey = "t4" });
        await dbContext.SaveChangesAsync();

        var sut = new ItemService(dbContext, new NoOpImageProcessor(), new NoOpFileStorage());

        var summary = await sut.GetSummaryAsync(userId);

        Assert.Equal(3, summary.TotalCount);
        Assert.Equal(300, summary.MaxItemsLimit);
        Assert.Equal(2, summary.Categories.Single(c => c.Code == "top").Count);
        Assert.Equal(1, summary.Categories.Single(c => c.Code == "shoes").Count);
        Assert.Equal(0, summary.Categories.Single(c => c.Code == "dress").Count);
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }

    private sealed class NoOpImageProcessor : IImageProcessorService
    {
        public bool ValidateImageSignature(Stream imageStream) => true;
        public Task<ProcessedImageResult> ProcessItemPhotoAsync(Stream imageStream, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<byte[]> ProcessAvatarAsync(Stream imageStream, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NoOpFileStorage : IFileStorageService
    {
        public Task<string> SaveFileAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Stream?> GetFileAsync(string fileKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
