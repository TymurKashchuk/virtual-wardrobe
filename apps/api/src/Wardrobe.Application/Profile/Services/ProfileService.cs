using Microsoft.EntityFrameworkCore;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Profile.Models;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Application.Profile.Services;

public class ProfileService : IProfileService
{
    private readonly IWardrobeDbContext _dbContext;

    public ProfileService(IWardrobeDbContext dbContext)
    {
        _dbContext = dbContext;
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
