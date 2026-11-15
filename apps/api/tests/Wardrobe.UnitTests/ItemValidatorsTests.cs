using Wardrobe.Application.Items.Models;
using Wardrobe.Application.Items.Validators;
using Wardrobe.Domain.Enums;

namespace Wardrobe.UnitTests;

public class ItemValidatorsTests
{
    private readonly CreateItemRequestValidator _createValidator = new();
    private readonly UpdateItemRequestValidator _updateValidator = new();

    [Fact]
    public async Task CreateValidator_RejectsMoreThanThreeColors()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new CreateItemRequest(
            "Jacket",
            1,
            1,
            [2, 3, 4],
            ClothingStyle.Casual,
            null,
            stream,
            "photo.jpg",
            1024);

        var result = await _createValidator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateValidator_RejectsDuplicatePrimaryColorInSecondaryColors()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new CreateItemRequest(
            "Jacket",
            1,
            1,
            [1, 2],
            ClothingStyle.Casual,
            null,
            stream,
            "photo.jpg",
            1024);

        var result = await _createValidator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateValidator_AllowsRequestWithoutNewPhoto()
    {
        var request = new UpdateItemRequest(
            "Updated Shirt",
            3,
            1,
            [2],
            ClothingStyle.Business,
            "Cotton");

        var result = await _updateValidator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateValidator_RejectsOversizedNewPhotoAndTooManyColors()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var request = new UpdateItemRequest(
            "Updated Shirt",
            3,
            1,
            [2, 3, 4],
            ClothingStyle.Business,
            null,
            stream,
            "huge.png",
            11 * 1024 * 1024);

        var result = await _updateValidator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateItemRequest.NewPhotoLength));
    }
}
