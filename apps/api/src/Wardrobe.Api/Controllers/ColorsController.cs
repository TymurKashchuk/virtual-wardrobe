using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Api.Controllers;

[ApiController]
[Route("api/v1/colors")]
public class ColorsController : ControllerBase
{
    private readonly IWardrobeDbContext _dbContext;

    public ColorsController(IWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetColors(CancellationToken cancellationToken)
    {
        var colors = await _dbContext.Colors
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.NameUk,
                c.Hex,
                c.IsNeutral,
                c.SortOrder
            })
            .ToListAsync(cancellationToken);

        return Ok(colors);
    }
}
