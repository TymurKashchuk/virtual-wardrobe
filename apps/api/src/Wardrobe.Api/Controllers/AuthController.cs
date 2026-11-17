using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    private readonly IValidator<ResendConfirmationRequest> _resendValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ResendConfirmationRequest> resendValidator,
        ICurrentUserService currentUserService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _resendValidator = resendValidator;
        _currentUserService = currentUserService;
        _environment = environment;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = ToValidationErrors(validationResult) });
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
    [EnableRateLimiting("auth-strict")]
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

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _resendValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = ToValidationErrors(validationResult) });
        }

        try
        {
            await _authService.ResendConfirmationEmailAsync(request, cancellationToken);
            return Ok(new
            {
                message = "If an account with this email exists and is not yet confirmed, a confirmation email has been sent."
            });
        }
        catch (RateLimitExceededException ex)
        {
            Response.Headers.RetryAfter = "60";
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = ToValidationErrors(validationResult) });
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
    [EnableRateLimiting("auth-refresh")]
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
    [EnableRateLimiting("auth-refresh")]
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

    private static Dictionary<string, string[]> ToValidationErrors(ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
    }
}

public record RefreshTokenRequestBody(string? RefreshToken);
