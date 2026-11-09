namespace Wardrobe.Application.Auth.Models;

public record LoginRequest(
    string Email,
    string Password
);
