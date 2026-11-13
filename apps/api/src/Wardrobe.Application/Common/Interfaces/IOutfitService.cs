using Wardrobe.Application.Common.Models;
using Wardrobe.Application.Outfits.Models;

namespace Wardrobe.Application.Common.Interfaces;

public interface IOutfitService
{
    Task<OutfitDto> CreateOutfitAsync(Guid userId, CreateOutfitRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<OutfitSummaryDto>> GetOutfitsAsync(Guid userId, OutfitFilterQuery query, CancellationToken cancellationToken = default);
    Task<OutfitDto?> GetOutfitByIdAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default);
    Task<OutfitDto> UpdateOutfitAsync(Guid userId, Guid outfitId, UpdateOutfitRequest request, CancellationToken cancellationToken = default);
    Task DeleteOutfitAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default);
    Task<OutfitDto> DuplicateOutfitAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default);
    Task<bool> ToggleFavoriteAsync(Guid userId, Guid outfitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutfitSummaryDto>> GetOutfitsByItemIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
}
