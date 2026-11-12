using Wardrobe.Application.Common.Models;
using Wardrobe.Application.Items.Models;

namespace Wardrobe.Application.Common.Interfaces;

public interface IItemService
{
    Task<ItemDto> CreateItemAsync(Guid userId, CreateItemRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ItemDto>> GetItemsAsync(Guid userId, ItemFilterQuery query, CancellationToken cancellationToken = default);
    Task<ItemDto?> GetItemByIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<ItemDto> UpdateItemAsync(Guid userId, Guid itemId, UpdateItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
}
