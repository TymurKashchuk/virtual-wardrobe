using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Models;
using Wardrobe.Application.Items.Models;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Application.Items.Services;

public class ItemService : IItemService
{
    private const int MaxItemsPerUser = 300;

    private readonly IWardrobeDbContext _dbContext;
    private readonly IImageProcessorService _imageProcessor;
    private readonly IFileStorageService _fileStorage;

    public ItemService(
        IWardrobeDbContext dbContext,
        IImageProcessorService imageProcessor,
        IFileStorageService fileStorage)
    {
        _dbContext = dbContext;
        _imageProcessor = imageProcessor;
        _fileStorage = fileStorage;
    }

    public async Task<ItemDto> CreateItemAsync(
        Guid userId, 
        CreateItemRequest request, 
        CancellationToken cancellationToken = default)
    {
        var itemsCount = await _dbContext.Items
            .CountAsync(i => i.UserId == userId, cancellationToken);

        if (itemsCount >= MaxItemsPerUser)
        {
            throw new InvalidOperationException($"Item limit reached. You cannot add more than {MaxItemsPerUser} items.");
        }

        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new InvalidOperationException("Selected category does not exist.");
        }

        var secondaryColors = request.SecondaryColorIds?.Distinct().ToList() ?? new List<short>();
        var allColorIds = secondaryColors.Append(request.PrimaryColorId).Distinct().ToList();

        var existingColorsCount = await _dbContext.Colors
            .CountAsync(c => allColorIds.Contains(c.Id), cancellationToken);

        if (existingColorsCount != allColorIds.Count)
        {
            throw new InvalidOperationException("One or more selected colors do not exist.");
        }

        if (!_imageProcessor.ValidateImageSignature(request.PhotoStream))
        {
            throw new InvalidOperationException("Uploaded file is not a valid image. Supported formats: JPEG, PNG, WebP.");
        }

        var processed = await _imageProcessor.ProcessItemPhotoAsync(request.PhotoStream, cancellationToken);

        var itemId = Guid.NewGuid();
        var photoKey = await _fileStorage.SaveFileAsync(
            $"items/{userId}/{itemId}.webp", 
            processed.PhotoBytes, 
            cancellationToken);

        var thumbKey = await _fileStorage.SaveFileAsync(
            $"items/{userId}/{itemId}_thumb.webp", 
            processed.ThumbnailBytes, 
            cancellationToken);

        var item = new Item
        {
            Id = itemId,
            UserId = userId,
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Style = request.Style,
            Notes = request.Notes?.Trim(),
            PhotoKey = photoKey,
            ThumbKey = thumbKey,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var itemColors = new List<ItemColor>
        {
            new() { ItemId = itemId, ColorId = request.PrimaryColorId, IsPrimary = true }
        };

        foreach (var colorId in secondaryColors.Where(id => id != request.PrimaryColorId))
        {
            itemColors.Add(new ItemColor { ItemId = itemId, ColorId = colorId, IsPrimary = false });
        }

        _dbContext.Items.Add(item);
        _dbContext.ItemColors.AddRange(itemColors);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteFilesSafelyAsync(photoKey, thumbKey);
            throw;
        }

        return await GetItemByIdAsync(userId, itemId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created item.");
    }

    public async Task<PagedResult<ItemDto>> GetItemsAsync(
        Guid userId, 
        ItemFilterQuery query, 
        CancellationToken cancellationToken = default)
    {
        var itemsQuery = _dbContext.Items
            .AsNoTracking()
            .Where(i => i.UserId == userId);

        if (query.CategoryIds != null && query.CategoryIds.Count > 0)
        {
            itemsQuery = itemsQuery.Where(i => query.CategoryIds.Contains(i.CategoryId));
        }

        if (query.ColorIds != null && query.ColorIds.Count > 0)
        {
            itemsQuery = itemsQuery.Where(i => i.ItemColors.Any(ic => query.ColorIds.Contains(ic.ColorId)));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search.Trim().ToLower()}%";
            itemsQuery = itemsQuery.Where(i => EF.Functions.Like(i.Name.ToLower(), searchPattern));
        }

        var totalCount = await itemsQuery.CountAsync(cancellationToken);

        itemsQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "name" => itemsQuery.OrderBy(i => i.Name),
            _ => itemsQuery.OrderByDescending(i => i.CreatedAt)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var items = await itemsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(i => i.Category)
            .Include(i => i.ItemColors)
                .ThenInclude(ic => ic.Color)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<ItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<WardrobeSummaryDto> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Code, c.NameUk })
            .ToListAsync(cancellationToken);

        var countsByCategory = await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .GroupBy(i => i.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var categorySummaries = categories
            .Select(c => new CategorySummaryDto(
                c.Id,
                c.Code,
                c.NameUk,
                countsByCategory.GetValueOrDefault(c.Id, 0)))
            .ToList();

        var totalCount = categorySummaries.Sum(c => c.Count);

        return new WardrobeSummaryDto(totalCount, MaxItemsPerUser, categorySummaries);
    }

    public async Task<ItemDto?> GetItemByIdAsync(
        Guid userId, 
        Guid itemId, 
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.ItemColors)
                .ThenInclude(ic => ic.Color)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<ItemDto> UpdateItemAsync(
        Guid userId, 
        Guid itemId, 
        UpdateItemRequest request, 
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Items
            .Include(i => i.ItemColors)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, cancellationToken);

        if (item == null)
        {
            throw new InvalidOperationException("Item not found.");
        }

        var categoryExists = await _dbContext.Categories
            .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new InvalidOperationException("Selected category does not exist.");
        }

        var secondaryColors = request.SecondaryColorIds?.Distinct().ToList() ?? new List<short>();
        var allColorIds = secondaryColors.Append(request.PrimaryColorId).Distinct().ToList();

        var existingColorsCount = await _dbContext.Colors
            .CountAsync(c => allColorIds.Contains(c.Id), cancellationToken);

        if (existingColorsCount != allColorIds.Count)
        {
            throw new InvalidOperationException("One or more selected colors do not exist.");
        }

        item.Name = request.Name.Trim();
        item.CategoryId = request.CategoryId;
        item.Style = request.Style;
        item.Notes = request.Notes?.Trim();
        item.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.ItemColors.RemoveRange(item.ItemColors);

        var newItemColors = new List<ItemColor>
        {
            new() { ItemId = itemId, ColorId = request.PrimaryColorId, IsPrimary = true }
        };

        foreach (var colorId in secondaryColors.Where(id => id != request.PrimaryColorId))
        {
            newItemColors.Add(new ItemColor { ItemId = itemId, ColorId = colorId, IsPrimary = false });
        }

        _dbContext.ItemColors.AddRange(newItemColors);

        string? oldPhotoKey = null;
        string? oldThumbKey = null;
        string? newPhotoKey = null;
        string? newThumbKey = null;

        if (request.NewPhotoStream != null)
        {
            if (!_imageProcessor.ValidateImageSignature(request.NewPhotoStream))
            {
                throw new InvalidOperationException("Uploaded file is not a valid image. Supported formats: JPEG, PNG, WebP.");
            }

            var processed = await _imageProcessor.ProcessItemPhotoAsync(request.NewPhotoStream, cancellationToken);
            var version = Guid.NewGuid().ToString("N")[..8];

            oldPhotoKey = item.PhotoKey;
            oldThumbKey = item.ThumbKey;

            newPhotoKey = await _fileStorage.SaveFileAsync(
                $"items/{userId}/{itemId}_{version}.webp",
                processed.PhotoBytes,
                cancellationToken);

            newThumbKey = await _fileStorage.SaveFileAsync(
                $"items/{userId}/{itemId}_{version}_thumb.webp",
                processed.ThumbnailBytes,
                cancellationToken);

            item.PhotoKey = newPhotoKey;
            item.ThumbKey = newThumbKey;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (newPhotoKey != null && newThumbKey != null)
            {
                await DeleteFilesSafelyAsync(newPhotoKey, newThumbKey);
            }
            throw;
        }

        if (oldPhotoKey != null && oldThumbKey != null)
        {
            await DeleteFilesSafelyAsync(oldPhotoKey, oldThumbKey);
        }

        return await GetItemByIdAsync(userId, itemId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated item.");
    }

    public async Task DeleteItemAsync(
        Guid userId, 
        Guid itemId, 
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Items
            .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == userId, cancellationToken);

        if (item == null)
        {
            return;
        }

        var photoKey = item.PhotoKey;
        var thumbKey = item.ThumbKey;

        var affectedOutfitIds = await _dbContext.OutfitItems
            .Where(oi => oi.ItemId == itemId)
            .Select(oi => oi.OutfitId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _dbContext.Items.Remove(item);

        if (affectedOutfitIds.Count > 0)
        {
            var outfitsToCheck = await _dbContext.Outfits
                .Where(o => affectedOutfitIds.Contains(o.Id))
                .Include(o => o.OutfitItems)
                .ToListAsync(cancellationToken);

            foreach (var outfit in outfitsToCheck)
            {
                var remainingCount = outfit.OutfitItems.Count(oi => oi.ItemId != itemId);
                if (remainingCount < 2)
                {
                    _dbContext.Outfits.Remove(outfit);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await DeleteFilesSafelyAsync(photoKey, thumbKey);
    }

    private async Task DeleteFilesSafelyAsync(params string[] fileKeys)
    {
        foreach (var fileKey in fileKeys)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
            {
                continue;
            }

            try
            {
                await _fileStorage.DeleteFileAsync(fileKey, CancellationToken.None);
            }
            catch
            {
                // Best-effort cleanup so orphaned-file errors do not mask the primary result.
            }
        }
    }

    private static ItemDto MapToDto(Item item)
    {
        var colors = item.ItemColors
            .OrderByDescending(ic => ic.IsPrimary)
            .Select(ic => new ItemColorDto(
                ic.ColorId,
                ic.Color?.Code ?? string.Empty,
                ic.Color?.NameUk ?? string.Empty,
                ic.Color?.Hex ?? string.Empty,
                ic.Color?.IsNeutral ?? false,
                ic.IsPrimary
            ))
            .ToList();

        return new ItemDto(
            item.Id,
            item.Name,
            item.CategoryId,
            item.Category?.Code ?? string.Empty,
            item.Category?.NameUk ?? string.Empty,
            item.Style,
            item.Notes,
            item.PhotoKey,
            item.ThumbKey,
            colors,
            item.CreatedAt,
            item.UpdatedAt
        );
    }
}
