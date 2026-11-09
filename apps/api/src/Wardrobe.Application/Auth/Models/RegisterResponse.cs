namespace Wardrobe.Application.Auth.Models;

public record RegisterResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string Message
);
