using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wardrobe.Application.Auth.Models;
using Wardrobe.Application.Auth.Services;
using Wardrobe.Application.Common.Exceptions;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Common.Options;
using Wardrobe.Domain.Entities;
using Wardrobe.Infrastructure.Persistence;
using Wardrobe.Infrastructure.Services;

namespace Wardrobe.UnitTests;

public class AuthServiceSecurityTests
{
    private static WardrobeDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WardrobeDbContext>()
            .UseInMemoryDatabase($"wardrobe_auth_tests_{Guid.NewGuid():N}")
            .Options;

        return new WardrobeDbContext(options);
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test_secret_key_for_unit_tests_at_least_32_bytes_long!",
                ["Jwt:Issuer"] = "WardrobeTest",
                ["Jwt:Audience"] = "WardrobeTestClients",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:EmailConfirmationExpirationHours"] = "24"
            })
            .Build();

        return new JwtTokenService(config);
    }

    [Fact]
    public async Task RegisterAsync_HtmlEncodesDisplayNameAndIncludesConfirmationLinkInEmailBody()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var emailSender = new CapturingEmailSenderService();
        var sut = new AuthService(
            dbContext,
            new PasswordHasherService(),
            CreateJwtTokenService(),
            emailSender,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new AppOptions { FrontendBaseUrl = "https://wardrobe.example.com/" }));

        await sut.RegisterAsync(new RegisterRequest(
            "attacker@example.com",
            "Password123",
            "<script>alert('xss')</script>"));

        Assert.Single(emailSender.SentEmails);
        var body = emailSender.SentEmails[0].HtmlBody;
        Assert.DoesNotContain("<script>", body);
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", body);
        Assert.Contains("https://wardrobe.example.com/confirm-email?token=", body);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRevokedTokenReused_RevokesAllActiveTokensForUserOnly()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var jwtService = CreateJwtTokenService();
        var emailSender = new CapturingEmailSenderService();
        var sut = new AuthService(
            dbContext,
            new PasswordHasherService(),
            jwtService,
            emailSender,
            new MemoryCache(new MemoryCacheOptions()));

        var victim = new User
        {
            Email = "victim@example.com",
            DisplayName = "Victim",
            PasswordHash = "hash",
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        var otherUser = new User
        {
            Email = "other@example.com",
            DisplayName = "Other",
            PasswordHash = "hash",
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.AddRange(victim, otherUser);

        var stolenRevokedRaw = "stolen-revoked-refresh-token";
        var victimActive1 = new RefreshToken
        {
            UserId = victim.Id,
            TokenHash = jwtService.HashToken("victim-active-1"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var victimActive2 = new RefreshToken
        {
            UserId = victim.Id,
            TokenHash = jwtService.HashToken("victim-active-2"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var victimRevoked = new RefreshToken
        {
            UserId = victim.Id,
            TokenHash = jwtService.HashToken(stolenRevokedRaw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var otherUserActive = new RefreshToken
        {
            UserId = otherUser.Id,
            TokenHash = jwtService.HashToken("other-active"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        dbContext.RefreshTokens.AddRange(victimActive1, victimActive2, victimRevoked, otherUserActive);
        await dbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AuthException>(
            () => sut.RefreshTokenAsync(stolenRevokedRaw));

        Assert.Contains("reuse detected", ex.Message, StringComparison.OrdinalIgnoreCase);

        var victimTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == victim.Id)
            .ToListAsync();
        Assert.All(victimTokens, t => Assert.NotNull(t.RevokedAt));

        var otherToken = await dbContext.RefreshTokens
            .SingleAsync(t => t.UserId == otherUser.Id);
        Assert.Null(otherToken.RevokedAt);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_EnforcesCooldownAndSendsOnlyForUnconfirmedUser()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var emailSender = new CapturingEmailSenderService();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new AuthService(
            dbContext,
            new PasswordHasherService(),
            CreateJwtTokenService(),
            emailSender,
            cache);

        dbContext.Users.Add(new User
        {
            Email = "unconfirmed@example.com",
            DisplayName = "Unconfirmed",
            PasswordHash = "hash",
            EmailConfirmedAt = null
        });
        dbContext.Users.Add(new User
        {
            Email = "confirmed@example.com",
            DisplayName = "Confirmed",
            PasswordHash = "hash",
            EmailConfirmedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        await sut.ResendConfirmationEmailAsync(new ResendConfirmationRequest("unconfirmed@example.com"));
        Assert.Single(emailSender.SentEmails);

        await Assert.ThrowsAsync<RateLimitExceededException>(() =>
            sut.ResendConfirmationEmailAsync(new ResendConfirmationRequest("unconfirmed@example.com")));

        await sut.ResendConfirmationEmailAsync(new ResendConfirmationRequest("confirmed@example.com"));
        await sut.ResendConfirmationEmailAsync(new ResendConfirmationRequest("missing@example.com"));
        Assert.Single(emailSender.SentEmails);

        // Even for a non-existent email, repeating within 1 minute triggers the same cooldown (no enumeration).
        await Assert.ThrowsAsync<RateLimitExceededException>(() =>
            sut.ResendConfirmationEmailAsync(new ResendConfirmationRequest("missing@example.com")));
    }

    [Fact]
    public async Task GoogleLoginAsync_CreatesNewUser_LinksConfirmedUser_AndBlocksUnconfirmedOrUnverified()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var emailSender = new CapturingEmailSenderService();
        var googleValidator = new StubGoogleTokenValidator();

        dbContext.Users.AddRange(
            new User
            {
                Email = "confirmed@example.com",
                DisplayName = "Confirmed User",
                PasswordHash = "hash",
                EmailConfirmedAt = DateTimeOffset.UtcNow
            },
            new User
            {
                Email = "pending@example.com",
                DisplayName = "Pending User",
                PasswordHash = "hash",
                EmailConfirmedAt = null
            });
        await dbContext.SaveChangesAsync();

        var sut = new AuthService(
            dbContext,
            new PasswordHasherService(),
            CreateJwtTokenService(),
            emailSender,
            new MemoryCache(new MemoryCacheOptions()),
            googleTokenValidator: googleValidator);

        // 1. New Google user
        googleValidator.NextPayload = new GoogleUserPayload("newgoogle@example.com", true, "Google User", null);
        var createdAuth = await sut.GoogleLoginAsync(new GoogleLoginRequest("token-new"));
        Assert.Equal("newgoogle@example.com", createdAuth.Email);
        var newUser = await dbContext.Users.SingleAsync(u => u.Email == "newgoogle@example.com");
        Assert.Null(newUser.PasswordHash);
        Assert.NotNull(newUser.EmailConfirmedAt);

        // 2. Existing confirmed user links cleanly
        googleValidator.NextPayload = new GoogleUserPayload("confirmed@example.com", true, "Changed Name", null);
        var linkedAuth = await sut.GoogleLoginAsync(new GoogleLoginRequest("token-confirmed"));
        Assert.Equal("Confirmed User", linkedAuth.DisplayName);

        // 3. Existing unconfirmed user is rejected with EmailNotConfirmedException
        googleValidator.NextPayload = new GoogleUserPayload("pending@example.com", true, "Pending User", null);
        await Assert.ThrowsAsync<EmailNotConfirmedException>(() =>
            sut.GoogleLoginAsync(new GoogleLoginRequest("token-pending")));

        // 4. Unverified Google email is rejected
        googleValidator.NextPayload = new GoogleUserPayload("unverified@example.com", false, "Unverified", null);
        await Assert.ThrowsAsync<AuthException>(() =>
            sut.GoogleLoginAsync(new GoogleLoginRequest("token-unverified")));
    }

    private sealed class StubGoogleTokenValidator : IGoogleTokenValidator
    {
        public GoogleUserPayload NextPayload { get; set; } = new("user@example.com", true, "User", null);

        public Task<GoogleUserPayload> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NextPayload);
        }
    }

    private sealed class CapturingEmailSenderService : IEmailSenderService
    {
        public List<(string Recipient, string Subject, string HtmlBody)> SentEmails { get; } = [];

        public Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            SentEmails.Add((recipientEmail, subject, htmlBody));
            return Task.CompletedTask;
        }
    }
}
