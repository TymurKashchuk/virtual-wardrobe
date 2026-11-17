using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Profile.Models;

namespace Wardrobe.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/me")]
public class MeController : ControllerBase
{
    private const long MaxUploadRequestBytes = 11 * 1024 * 1024;

    private readonly IProfileService _profileService;
    private readonly IFileStorageService _fileStorage;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly IWebHostEnvironment _environment;

    public MeController(
        IProfileService profileService,
        IFileStorageService fileStorage,
        ICurrentUserService currentUserService,
        IValidator<UpdateProfileRequest> updateProfileValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        IWebHostEnvironment environment)
    {
        _profileService = profileService;
        _fileStorage = fileStorage;
        _currentUserService = currentUserService;
        _updateProfileValidator = updateProfileValidator;
        _changePasswordValidator = changePasswordValidator;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            var profile = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var validationResult = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = ToValidationErrors(validationResult) });
        }

        try
        {
            var profile = await _profileService.UpdateProfileAsync(userId.Value, request, cancellationToken);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAvatar(
        IFormFile? avatar,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        if (avatar == null || avatar.Length == 0)
        {
            return BadRequest(new { message = "Avatar file is required." });
        }

        await using var stream = avatar.OpenReadStream();

        try
        {
            var profile = await _profileService.UpdateAvatarAsync(
                userId.Value,
                stream,
                avatar.Length,
                cancellationToken);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvatar(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var avatarKey = await _profileService.GetAvatarKeyAsync(userId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(avatarKey))
        {
            return NotFound();
        }

        var stream = await _fileStorage.GetFileAsync(avatarKey, cancellationToken);
        if (stream == null)
        {
            return NotFound();
        }

        return File(stream, "image/webp");
    }

    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await _profileService.DeleteAvatarAsync(userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var validationResult = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = ToValidationErrors(validationResult) });
        }

        try
        {
            await _profileService.ChangePasswordAsync(userId.Value, request, cancellationToken);
            DeleteRefreshTokenCookie();
            return Ok(new { message = "Password updated successfully. Please log in again." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        try
        {
            await _profileService.DeleteAccountAsync(userId.Value, cancellationToken);
            DeleteRefreshTokenCookie();
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }

    private static Dictionary<string, string[]> ToValidationErrors(ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
    }
}
