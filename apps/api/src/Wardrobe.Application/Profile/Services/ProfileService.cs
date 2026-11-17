using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Profile.Models;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Application.Profile.Services;

public class ProfileService : IProfileService
{
    private const long MaxAvatarSizeBytes = 10 * 1024 * 1024;

    private readonly IWardrobeDbContext _dbContext;
    private readonly IImageProcessorService _imageProcessor;
    private readonly IFileStorageService _fileStorage;
    private readonly IPasswordHasherService _passwordHasher;

    public ProfileService(
        IWardrobeDbContext dbContext,
        IImageProcessorService imageProcessor,
        IFileStorageService fileStorage,
        IPasswordHasherService passwordHasher)
    {
        _dbContext = dbContext;
        _imageProcessor = imageProcessor;
        _fileStorage = fileStorage;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileDto> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return MapToDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.DisplayName = request.DisplayName.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    public async Task<UserProfileDto> UpdateAvatarAsync(
        Guid userId,
        Stream photoStream,
        long fileLength,
        CancellationToken cancellationToken = default)
    {
        if (fileLength <= 0 || fileLength > MaxAvatarSizeBytes)
        {
            throw new InvalidOperationException("Avatar file size must be between 1 byte and 10 MB.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!_imageProcessor.ValidateImageSignature(photoStream))
        {
            throw new InvalidOperationException("Uploaded file is not a valid image. Supported formats: JPEG, PNG, WebP.");
        }

        var avatarBytes = await _imageProcessor.ProcessAvatarAsync(photoStream, cancellationToken);
        var version = Guid.NewGuid().ToString("N")[..8];
        var oldAvatarKey = user.AvatarKey;

        var newAvatarKey = await _fileStorage.SaveFileAsync(
            $"avatars/{userId}/{version}.webp",
            avatarBytes,
            cancellationToken);

        user.AvatarKey = newAvatarKey;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteFileSafelyAsync(newAvatarKey);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldAvatarKey))
        {
            await DeleteFileSafelyAsync(oldAvatarKey);
        }

        return MapToDto(user);
    }

    public async Task<string?> GetAvatarKeyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.AvatarKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAvatarAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrWhiteSpace(user.AvatarKey))
        {
            return;
        }

        var oldAvatarKey = user.AvatarKey;
        user.AvatarKey = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await DeleteFileSafelyAsync(oldAvatarKey);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = now;

        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var itemMedia = await _dbContext.Items
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .Select(i => new { i.PhotoKey, i.ThumbKey, i.PhotoCutoutKey })
            .ToListAsync(cancellationToken);

        var filesToDelete = new List<string>();
        if (!string.IsNullOrWhiteSpace(user.AvatarKey))
        {
            filesToDelete.Add(user.AvatarKey);
        }

        foreach (var media in itemMedia)
        {
            if (!string.IsNullOrWhiteSpace(media.PhotoKey))
            {
                filesToDelete.Add(media.PhotoKey);
            }
            if (!string.IsNullOrWhiteSpace(media.ThumbKey))
            {
                filesToDelete.Add(media.ThumbKey);
            }
            if (!string.IsNullOrWhiteSpace(media.PhotoCutoutKey))
            {
                filesToDelete.Add(media.PhotoCutoutKey);
            }
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var fileKey in filesToDelete)
        {
            await DeleteFileSafelyAsync(fileKey);
        }
    }

    private async Task DeleteFileSafelyAsync(string fileKey)
    {
        try
        {
            await _fileStorage.DeleteFileAsync(fileKey, CancellationToken.None);
        }
        catch
        {
            // Best-effort cleanup so orphaned-file errors do not mask the primary operation.
        }
    }

    private static UserProfileDto MapToDto(User user)
    {
        return new UserProfileDto(
            user.Id,
            user.Email,
            user.DisplayName,
            !string.IsNullOrWhiteSpace(user.AvatarKey),
            !string.IsNullOrEmpty(user.PasswordHash),
            user.Role,
            user.CreatedAt);
    }
}
