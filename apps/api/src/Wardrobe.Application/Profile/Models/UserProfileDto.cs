using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Profile.Models;

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool HasAvatar,
    bool HasPassword,
    UserRole Role,
    DateTimeOffset CreatedAt
);
