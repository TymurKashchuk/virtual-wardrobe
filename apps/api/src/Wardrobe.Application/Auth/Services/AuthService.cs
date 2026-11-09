using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Auth.Models;
using Wardrobe.Application.Common.Exceptions;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IWardrobeDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSenderService _emailSenderService;

    public AuthService(
        IWardrobeDbContext dbContext,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService,
        IEmailSenderService emailSenderService)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
        _emailSenderService = emailSenderService;
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

        var confirmationToken = _jwtTokenService.GenerateEmailConfirmationToken(user);

        var emailSubject = "Підтвердження реєстрації у Virtual Wardrobe";
        var emailBody = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h2 style="color: #333;">Ласкаво просимо до Virtual Wardrobe!</h2>
                <p>Привіт, <strong>{user.DisplayName}</strong>!</p>
                <p>Дякуємо за реєстрацію. Будь ласка, підтвердіть вашу адресу електронної пошти.</p>
                <div style="background-color: #f8f9fa; border: 1px solid #e9ecef; border-radius: 6px; padding: 15px; margin: 20px 0;">
                    <p style="margin: 0 0 10px 0; font-weight: bold;">Ваш одноразовий токен підтвердження (діє 24 години):</p>
                    <code style="word-break: break-all; color: #0d6efd; font-size: 14px;">{confirmationToken}</code>
                </div>
                <p style="color: #6c757d; font-size: 13px;">Якщо ви не створювали акаунт у Virtual Wardrobe, просто проігноруйте цей лист.</p>
            </div>
            """;

        await _emailSenderService.SendEmailAsync(user.Email, emailSubject, emailBody, cancellationToken);

        return new RegisterResponse(
            user.Id, 
            user.Email, 
            user.DisplayName, 
            "Registration successful. Please check your email to confirm your account.");
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

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _jwtTokenService.HashToken(refreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || !existingToken.IsActive || existingToken.User == null)
        {
            throw new AuthException("Invalid or expired refresh token.");
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashToken(newRefreshToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var newAccessToken = _jwtTokenService.GenerateAccessToken(existingToken.User);

        return new AuthResponse(
            newAccessToken,
            newRefreshToken,
            existingToken.User.Id,
            existingToken.User.Email,
            existingToken.User.DisplayName);
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
}
