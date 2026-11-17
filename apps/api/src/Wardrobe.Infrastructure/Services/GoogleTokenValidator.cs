using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Wardrobe.Application.Common.Exceptions;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Options;

namespace Wardrobe.Infrastructure.Services;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options;

    public GoogleTokenValidator(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public async Task<GoogleUserPayload> ValidateIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Google:ClientId is not configured.");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserPayload(
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException)
        {
            throw new AuthException("Invalid or expired Google ID token.");
        }
    }
}
