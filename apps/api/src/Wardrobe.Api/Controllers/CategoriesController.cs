using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IWardrobeDbContext _dbContext;

    public CategoriesController(IWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.NameUk,
                c.MaxPerOutfit,
                c.SortOrder
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }
}
