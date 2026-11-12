namespace Wardrobe.Application.Items.Models;

public record ItemFilterQuery(
    IReadOnlyList<short>? CategoryIds = null,
    IReadOnlyList<short>? ColorIds = null,
    string? Search = null,
    string? SortBy = "newest",
    int Page = 1,
    int PageSize = 24
);
