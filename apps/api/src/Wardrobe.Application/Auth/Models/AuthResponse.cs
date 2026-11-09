namespace Wardrobe.Application.Auth.Models;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Email,
    string DisplayName
);
