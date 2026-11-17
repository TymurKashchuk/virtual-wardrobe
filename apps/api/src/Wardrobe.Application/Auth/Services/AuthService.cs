using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Wardrobe.Application.Auth.Models;
using Wardrobe.Application.Common.Exceptions;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Options;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IWardrobeDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IMemoryCache _memoryCache;
    private readonly IGoogleTokenValidator? _googleTokenValidator;
    private readonly AppOptions _appOptions;

    public AuthService(
        IWardrobeDbContext dbContext,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService,
        IEmailSenderService emailSenderService,
        IMemoryCache memoryCache,
        IOptions<AppOptions>? appOptions = null,
        IGoogleTokenValidator? googleTokenValidator = null)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
        _emailSenderService = emailSenderService;
        _memoryCache = memoryCache;
        _appOptions = appOptions?.Value ?? new AppOptions();
        _googleTokenValidator = googleTokenValidator;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            throw new AuthException("User with this email already exists.");
        }

        var passwordHash = _passwordHasherService.HashPassword(request.Password);

        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.User,
            EmailConfirmedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await SendConfirmationEmailAsync(user, cancellationToken);
        _memoryCache.Set(GetResendCooldownKey(normalizedEmail), true, TimeSpan.FromMinutes(1));

        return new RegisterResponse(
            user.Id, 
            user.Email, 
            user.DisplayName, 
            "Registration successful. Please check your email to confirm your account.");
    }

    private async Task SendConfirmationEmailAsync(User user, CancellationToken cancellationToken)
    {
        var confirmationToken = _jwtTokenService.GenerateEmailConfirmationToken(user);
        var safeDisplayName = WebUtility.HtmlEncode(user.DisplayName);
        var baseUrl = _appOptions.FrontendBaseUrl.TrimEnd('/');
        var confirmationUrl = $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";
        var safeConfirmationUrl = WebUtility.HtmlEncode(confirmationUrl);

        var emailSubject = "Підтвердження реєстрації у Virtual Wardrobe";
        var emailBody = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h2 style="color: #333;">Ласкаво просимо до Virtual Wardrobe!</h2>
                <p>Привіт, <strong>{safeDisplayName}</strong>!</p>
                <p>Дякуємо за реєстрацію. Натисніть кнопку нижче, щоб підтвердити вашу адресу електронної пошти (посилання діє 24 години):</p>
                <p style="margin: 24px 0;">
                    <a href="{safeConfirmationUrl}" style="background-color: #0f172a; color: #ffffff; text-decoration: none; padding: 12px 20px; border-radius: 8px; display: inline-block; font-weight: bold;">
                        Підтвердити email
                    </a>
                </p>
                <div style="background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 6px; padding: 15px; margin: 20px 0;">
                    <p style="margin: 0 0 8px 0; font-size: 13px; font-weight: bold;">Якщо кнопка не працює, використайте токен:</p>
                    <code style="word-break: break-all; color: #0d6efd; font-size: 13px;">{confirmationToken}</code>
                </div>
                <p style="color: #6c757d; font-size: 13px;">Якщо ви не створювали акаунт у Virtual Wardrobe, просто проігноруйте цей лист.</p>
            </div>
            """;

        await _emailSenderService.SendEmailAsync(user.Email, emailSubject, emailBody, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasherService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AuthException("Invalid email or password.");
        }

        if (user.EmailConfirmedAt == null)
        {
            throw new EmailNotConfirmedException("Email is not confirmed. Please confirm your email address before logging in.");
        }

        return await IssueAuthTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (_googleTokenValidator == null)
        {
            throw new InvalidOperationException("Google token validator is not configured.");
        }

        var payload = await _googleTokenValidator.ValidateIdTokenAsync(request.IdToken, cancellationToken);
        if (!payload.EmailVerified)
        {
            throw new AuthException("Google account email is not verified.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user != null)
        {
            if (user.EmailConfirmedAt == null)
            {
                throw new EmailNotConfirmedException(
                    "An account with this email already exists and is awaiting email confirmation. Please confirm your email first to link your Google account.");
            }
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            user = new User
            {
                Email = normalizedEmail,
                DisplayName = ResolveDisplayName(payload.Name, normalizedEmail),
                PasswordHash = null,
                Role = UserRole.User,
                EmailConfirmedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Users.Add(user);
        }

        return await IssueAuthTokensAsync(user, cancellationToken);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _jwtTokenService.ValidateEmailConfirmationToken(request.Token);

        if (userId == null)
        {
            throw new AuthException("Invalid or expired email confirmation token.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            throw new AuthException("User not found.");
        }

        if (user.EmailConfirmedAt != null)
        {
            return;
        }

        user.EmailConfirmedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResendConfirmationEmailAsync(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var cooldownKey = GetResendCooldownKey(normalizedEmail);

        if (_memoryCache.TryGetValue(cooldownKey, out _))
        {
            throw new RateLimitExceededException(
                "Confirmation email was recently requested. Please wait 1 minute before trying again.");
        }

        _memoryCache.Set(cooldownKey, true, TimeSpan.FromMinutes(1));

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || user.EmailConfirmedAt != null)
        {
            return;
        }

        await SendConfirmationEmailAsync(user, cancellationToken);
    }

    private static string GetResendCooldownKey(string normalizedEmail) => $"resend_confirm:{normalizedEmail}";

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashToken(refreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || existingToken.User == null)
        {
            throw new AuthException("Invalid or expired refresh token.");
        }

        if (existingToken.RevokedAt != null)
        {
            await RevokeAllUserRefreshTokensAsync(existingToken.UserId, cancellationToken);
            throw new AuthException("Refresh token reuse detected. Please log in again.");
        }

        if (!existingToken.IsActive)
        {
            throw new AuthException("Invalid or expired refresh token.");
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;

        return await IssueAuthTokensAsync(existingToken.User, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashToken(refreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.RevokedAt == null, cancellationToken);

        if (existingToken != null)
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthResponse> IssueAuthTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashToken(refreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.Id,
            user.Email,
            user.DisplayName);
    }

    private static string ResolveDisplayName(string? name, string normalizedEmail)
    {
        var candidate = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : normalizedEmail.Split('@')[0];

        return candidate.Length > 100 ? candidate[..100] : candidate;
    }

    private async Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
