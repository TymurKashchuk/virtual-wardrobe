using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Models;
using Wardrobe.Application.Outfits.Models;

namespace Wardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/outfits")]
public class OutfitsController : ControllerBase
{
    private readonly IOutfitService _outfitService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateOutfitRequest> _createValidator;
    private readonly IValidator<UpdateOutfitRequest> _updateValidator;

    public OutfitsController(
        IOutfitService outfitService,
        ICurrentUserService currentUserService,
        IValidator<CreateOutfitRequest> createValidator,
        IValidator<UpdateOutfitRequest> updateValidator)
    {
        _outfitService = outfitService;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OutfitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOutfit(
        [FromBody] CreateOutfitRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _outfitService.CreateOutfitAsync(userId.Value, request, cancellationToken);
            return CreatedAtAction(nameof(GetOutfit), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OutfitSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOutfits(
        [FromQuery] OutfitFilterQuery query, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _outfitService.GetOutfitsAsync(userId.Value, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetOutfit))]
    [ProducesResponseType(typeof(OutfitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOutfit(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _outfitService.GetOutfitByIdAsync(userId.Value, id, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Outfit with ID '{id}' was not found." });
        }

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OutfitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOutfit(
        Guid id, 
        [FromBody] UpdateOutfitRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var result = await _outfitService.UpdateOutfitAsync(userId.Value, id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteOutfit(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        await _outfitService.DeleteOutfitAsync(userId.Value, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(OutfitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DuplicateOutfit(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var result = await _outfitService.DuplicateOutfitAsync(userId.Value, id, cancellationToken);
            return CreatedAtAction(nameof(GetOutfit), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/favorite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFavorite(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var isFavorite = await _outfitService.ToggleFavoriteAsync(userId.Value, id, cancellationToken);
            return Ok(new { isFavorite });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
