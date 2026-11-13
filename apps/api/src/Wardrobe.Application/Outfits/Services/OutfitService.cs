using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Models;
using Wardrobe.Application.Outfits.Models;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Application.Outfits.Services;

public class OutfitService : IOutfitService
{
    private readonly IWardrobeDbContext _dbContext;

    public OutfitService(IWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OutfitDto> CreateOutfitAsync(Guid userId, CreateOutfitRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAndGetOutfitItemsAsync(userId, request.ItemIds, cancellationToken);

        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Style = request.Style,
            Notes = request.Notes?.Trim(),
            IsFavorite = false,
            Source = request.Source,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        short position = 0;
        foreach (var itemId in request.ItemIds)
        {
            outfit.OutfitItems.Add(new OutfitItem
            {
                OutfitId = outfit.Id,
                ItemId = itemId,
                Position = position++
            });
        }

        _dbContext.Outfits.Add(outfit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetOutfitByIdAsync(userId, outfit.Id, cancellationToken))!;
    }

    public async Task<PagedResult<OutfitSummaryDto>> GetOutfitsAsync(Guid userId, OutfitFilterQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.Outfits
            .AsNoTracking()
            .Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchPattern = $"%{query.Search.Trim().ToLower()}%";
            queryable = queryable.Where(o => EF.Functions.Like(o.Name.ToLower(), searchPattern));
        }

        if (query.IsFavorite.HasValue)
        {
            queryable = queryable.Where(o => o.IsFavorite == query.IsFavorite.Value);
        }

        if (query.Style.HasValue)
        {
            queryable = queryable.Where(o => o.Style == query.Style.Value);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var outfits = await queryable
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new OutfitSummaryDto(
                o.Id,
                o.Name,
                o.Style,
                o.IsFavorite,
                o.Source,
                o.OutfitItems.Count,
                o.OutfitItems
                    .OrderBy(oi => oi.Position)
                    .Select(oi => oi.Item!.ThumbKey)
                    .Where(k => k != null)
                    .Select(k => k!)
                    .ToList(),
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<OutfitSummaryDto>
        {
            Items = outfits,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<OutfitDto?> GetOutfitByIdAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default)
    {
        var outfit = await _dbContext.Outfits
            .AsNoTracking()
            .Where(o => o.Id == outfitId && o.UserId == userId)
            .Include(o => o.OutfitItems)
                .ThenInclude(oi => oi.Item)
                    .ThenInclude(i => i!.Category)
            .Include(o => o.OutfitItems)
                .ThenInclude(oi => oi.Item)
                    .ThenInclude(i => i!.ItemColors)
                        .ThenInclude(ic => ic.Color)
            .FirstOrDefaultAsync(cancellationToken);

        if (outfit == null)
        {
            return null;
        }

        var items = outfit.OutfitItems
            .OrderBy(oi => oi.Position)
            .Where(oi => oi.Item != null && oi.Item.Category != null)
            .Select(oi => new OutfitItemDto(
                oi.ItemId,
                oi.Item!.Name,
                oi.Item.CategoryId,
                oi.Item.Category!.Code,
                oi.Item.Category.NameUk,
                oi.Item.ThumbKey,
                oi.Position,
                oi.Item.ItemColors.FirstOrDefault(c => c.IsPrimary)?.Color?.Hex
            ))
            .ToList();

        return new OutfitDto(
            outfit.Id,
            outfit.Name,
            outfit.Style,
            outfit.Notes,
            outfit.IsFavorite,
            outfit.Source,
            items,
            outfit.CreatedAt,
            outfit.UpdatedAt
        );
    }

    public async Task<OutfitDto> UpdateOutfitAsync(Guid userId, Guid outfitId, UpdateOutfitRequest request, CancellationToken cancellationToken = default)
    {
        var outfit = await _dbContext.Outfits
            .Where(o => o.Id == outfitId && o.UserId == userId)
            .Include(o => o.OutfitItems)
            .FirstOrDefaultAsync(cancellationToken);

        if (outfit == null)
        {
            throw new KeyNotFoundException($"Outfit with ID '{outfitId}' was not found.");
        }

        await ValidateAndGetOutfitItemsAsync(userId, request.ItemIds, cancellationToken);

        outfit.Name = request.Name.Trim();
        outfit.Style = request.Style;
        outfit.Notes = request.Notes?.Trim();
        outfit.UpdatedAt = DateTimeOffset.UtcNow;

        outfit.OutfitItems.Clear();
        short position = 0;
        foreach (var itemId in request.ItemIds)
        {
            outfit.OutfitItems.Add(new OutfitItem
            {
                OutfitId = outfit.Id,
                ItemId = itemId,
                Position = position++
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetOutfitByIdAsync(userId, outfit.Id, cancellationToken))!;
    }

    public async Task DeleteOutfitAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default)
    {
        var outfit = await _dbContext.Outfits
            .Where(o => o.Id == outfitId && o.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (outfit != null)
        {
            _dbContext.Outfits.Remove(outfit);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<OutfitDto> DuplicateOutfitAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default)
    {
        var outfit = await _dbContext.Outfits
            .AsNoTracking()
            .Where(o => o.Id == outfitId && o.UserId == userId)
            .Include(o => o.OutfitItems)
            .FirstOrDefaultAsync(cancellationToken);

        if (outfit == null)
        {
            throw new KeyNotFoundException($"Outfit with ID '{outfitId}' was not found.");
        }

        var copyName = $"Копія {outfit.Name}";
        if (copyName.Length > 80)
        {
            copyName = copyName.Substring(0, 80);
        }

        var newOutfit = new Outfit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = copyName,
            Style = outfit.Style,
            Notes = outfit.Notes,
            IsFavorite = false,
            Source = outfit.Source,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in outfit.OutfitItems.OrderBy(oi => oi.Position))
        {
            newOutfit.OutfitItems.Add(new OutfitItem
            {
                OutfitId = newOutfit.Id,
                ItemId = item.ItemId,
                Position = item.Position
            });
        }

        _dbContext.Outfits.Add(newOutfit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetOutfitByIdAsync(userId, newOutfit.Id, cancellationToken))!;
    }

    public async Task<bool> ToggleFavoriteAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default)
    {
        var outfit = await _dbContext.Outfits
            .Where(o => o.Id == outfitId && o.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (outfit == null)
        {
            throw new KeyNotFoundException($"Outfit with ID '{outfitId}' was not found.");
        }

        outfit.IsFavorite = !outfit.IsFavorite;
        outfit.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return outfit.IsFavorite;
    }

    public async Task<IReadOnlyList<OutfitSummaryDto>> GetOutfitsByItemIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var outfits = await _dbContext.Outfits
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.OutfitItems.Any(oi => oi.ItemId == itemId))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OutfitSummaryDto(
                o.Id,
                o.Name,
                o.Style,
                o.IsFavorite,
                o.Source,
                o.OutfitItems.Count,
                o.OutfitItems
                    .OrderBy(oi => oi.Position)
                    .Select(oi => oi.Item!.ThumbKey)
                    .Where(k => k != null)
                    .Select(k => k!)
                    .ToList(),
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return outfits;
    }

    private async Task<List<Item>> ValidateAndGetOutfitItemsAsync(Guid userId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
    {
        var distinctIds = itemIds.Distinct().ToList();
        var items = await _dbContext.Items
            .Include(i => i.Category)
            .Where(i => i.UserId == userId && distinctIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        if (items.Count != distinctIds.Count)
        {
            throw new InvalidOperationException("One or more items do not exist or do not belong to the user.");
        }

        var hasDress = items.Any(i => i.Category?.Code == "dress");
        var hasTop = items.Any(i => i.Category?.Code == "top");
        var hasBottom = items.Any(i => i.Category?.Code == "bottom");

        if (hasDress && (hasTop || hasBottom))
        {
            throw new InvalidOperationException("An outfit cannot combine a dress with tops or bottoms.");
        }

        var categoryCounts = items
            .Where(i => i.Category != null)
            .GroupBy(i => i.Category!.Code)
            .ToDictionary(g => g.Key, g => g.Count());

        if (categoryCounts.TryGetValue("outerwear", out var outerwearCount) && outerwearCount > 1)
        {
            throw new InvalidOperationException("An outfit cannot contain more than 1 outerwear item.");
        }

        if (categoryCounts.TryGetValue("dress", out var dressCount) && dressCount > 1)
        {
            throw new InvalidOperationException("An outfit cannot contain more than 1 dress.");
        }

        if (categoryCounts.TryGetValue("top", out var topCount) && topCount > 1)
        {
            throw new InvalidOperationException("An outfit cannot contain more than 1 top item.");
        }

        if (categoryCounts.TryGetValue("bottom", out var bottomCount) && bottomCount > 1)
        {
            throw new InvalidOperationException("An outfit cannot contain more than 1 bottom item.");
        }

        if (categoryCounts.TryGetValue("shoes", out var shoesCount) && shoesCount > 1)
        {
            throw new InvalidOperationException("An outfit cannot contain more than 1 pair of shoes.");
        }

        if (categoryCounts.TryGetValue("accessories", out var accessoriesCount) && accessoriesCount > 3)
        {
            throw new InvalidOperationException("An outfit cannot contain more than 3 accessories.");
        }

        return items;
    }
}
