namespace Wardrobe.Application.Items.Models;

public record CategorySummaryDto(
    short CategoryId,
    string Code,
    string NameUk,
    int Count
);

public record WardrobeSummaryDto(
    int TotalCount,
    int MaxItemsLimit,
    IReadOnlyList<CategorySummaryDto> Categories
);
