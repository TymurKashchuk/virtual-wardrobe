using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wardrobe.Application.Auth.Models;
using Wardrobe.Application.Common.Exceptions;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _currentUserService = currentUserService;
        _environment = environment;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new { errors });
        }

        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        try
        {
            await _authService.ConfirmEmailAsync(request, cancellationToken);
            return Ok(new { message = "Email confirmed successfully." });
        }
        catch (AuthException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new { errors });
        }

        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(new
            {
                response.AccessToken,
                response.RefreshToken,
                response.UserId,
                response.Email,
                response.DisplayName
            });
        }
        catch (EmailNotConfirmedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "email_not_confirmed",
                message = ex.Message
            });
        }
        catch (AuthException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var token = await ExtractRefreshTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        try
        {
            var response = await _authService.RefreshTokenAsync(token, cancellationToken);
            SetRefreshTokenCookie(response.RefreshToken);

            return Ok(new
            {
                response.AccessToken,
                response.RefreshToken,
                response.UserId,
                response.Email,
                response.DisplayName
            });
        }
        catch (AuthException ex)
        {
            DeleteRefreshTokenCookie();
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var token = await ExtractRefreshTokenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(token))
        {
            await _authService.LogoutAsync(token, cancellationToken);
        }

        DeleteRefreshTokenCookie();
        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            UserId = _currentUserService.UserId,
            IsAuthenticated = _currentUserService.IsAuthenticated
        });
    }

    private async Task<string?> ExtractRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var cookieToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (Request.HasJsonContentType() && Request.ContentLength is > 0)
        {
            try
            {
                var body = await Request.ReadFromJsonAsync<RefreshTokenRequestBody>(cancellationToken);
                return body?.RefreshToken;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            BuildRefreshTokenCookieOptions(DateTimeOffset.UtcNow.AddDays(30)));
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", BuildRefreshTokenCookieOptions());
    }

    private CookieOptions BuildRefreshTokenCookieOptions(DateTimeOffset? expires = null)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = expires
        };
    }
}

public record RefreshTokenRequestBody(string? RefreshToken);
