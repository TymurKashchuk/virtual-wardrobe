namespace Wardrobe.Application.Profile.Models;

public record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword
);
