using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Items.Models;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/items")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateItemRequest> _createValidator;

    public ItemsController(
        IItemService itemService,
        IFileStorageService fileStorage,
        ICurrentUserService currentUserService,
        IValidator<CreateItemRequest> createValidator)
    {
        _itemService = itemService;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
        _createValidator = createValidator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateItem(
        [FromForm] CreateItemForm form, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        if (form.Photo == null || form.Photo.Length == 0)
        {
            return BadRequest(new { message = "Photo file is required." });
        }

        await using var stream = form.Photo.OpenReadStream();

        var request = new CreateItemRequest(
            form.Name,
            form.CategoryId,
            form.PrimaryColorId,
            form.SecondaryColorIds,
            form.Style,
            form.Notes,
            stream,
            form.Photo.FileName,
            form.Photo.Length
        );

        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new { errors });
        }

        try
        {
            var result = await _itemService.CreateItemAsync(userId.Value, request, cancellationToken);
            return CreatedAtAction(nameof(GetItemById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(
        [FromQuery] ItemFilterQuery query, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _itemService.GetItemsAsync(userId.Value, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetItemById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var item = await _itemService.GetItemByIdAsync(userId.Value, id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { message = "Item not found." });
        }

        return Ok(item);
    }

    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> GetItemPhoto(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var item = await _itemService.GetItemByIdAsync(userId.Value, id, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        var stream = await _fileStorage.GetFileAsync(item.PhotoKey, cancellationToken);
        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, "image/webp");
    }

    [HttpGet("{id:guid}/thumb")]
    public async Task<IActionResult> GetItemThumb(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var item = await _itemService.GetItemByIdAsync(userId.Value, id, cancellationToken);
        if (item == null)
        {
            return NotFound();
        }

        var stream = await _fileStorage.GetFileAsync(item.ThumbKey, cancellationToken);
        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, "image/webp");
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateItem(
        Guid id, 
        [FromForm] UpdateItemForm form, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        Stream? newStream = null;
        if (form.NewPhoto != null && form.NewPhoto.Length > 0)
        {
            newStream = form.NewPhoto.OpenReadStream();
        }

        var request = new UpdateItemRequest(
            form.Name,
            form.CategoryId,
            form.PrimaryColorId,
            form.SecondaryColorIds,
            form.Style,
            form.Notes,
            newStream,
            form.NewPhoto?.FileName,
            form.NewPhoto?.Length
        );

        try
        {
            var result = await _itemService.UpdateItemAsync(userId.Value, id, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            if (newStream != null)
            {
                await newStream.DisposeAsync();
            }
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteItem(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        await _itemService.DeleteItemAsync(userId.Value, id, cancellationToken);
        return NoContent();
    }
}

public class CreateItemForm
{
    public string Name { get; set; } = string.Empty;
    public short CategoryId { get; set; }
    public short PrimaryColorId { get; set; }
    public List<short>? SecondaryColorIds { get; set; }
    public ClothingStyle Style { get; set; } = ClothingStyle.Casual;
    public string? Notes { get; set; }
    public IFormFile? Photo { get; set; }
}

public class UpdateItemForm
{
    public string Name { get; set; } = string.Empty;
    public short CategoryId { get; set; }
    public short PrimaryColorId { get; set; }
    public List<short>? SecondaryColorIds { get; set; }
    public ClothingStyle Style { get; set; } = ClothingStyle.Casual;
    public string? Notes { get; set; }
    public IFormFile? NewPhoto { get; set; }
}
