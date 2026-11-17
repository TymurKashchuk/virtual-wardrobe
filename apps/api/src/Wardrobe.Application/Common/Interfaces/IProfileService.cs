using Wardrobe.Application.Profile.Models;

namespace Wardrobe.Application.Common.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateAvatarAsync(Guid userId, Stream photoStream, long fileLength, CancellationToken cancellationToken = default);
    Task<string?> GetAvatarKeyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAvatarAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(Guid userId, CancellationToken cancellationToken = default);
}
