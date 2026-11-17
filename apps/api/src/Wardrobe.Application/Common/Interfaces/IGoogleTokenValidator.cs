namespace Wardrobe.Application.Common.Interfaces;

public record GoogleUserPayload(
    string Email,
    bool EmailVerified,
    string? Name,
    string? PictureUrl
);

public interface IGoogleTokenValidator
{
    Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
