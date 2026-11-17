using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Application.Profile.Models;
using Wardrobe.Application.Profile.Services;
using Wardrobe.Domain.Entities;
using Wardrobe.Domain.Enums;
using Wardrobe.Infrastructure.Persistence;
using Wardrobe.Infrastructure.Services;

namespace Wardrobe.UnitTests;

public class ProfileServiceTests
{
    private static WardrobeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WardrobeDbContext>()
            .UseInMemoryDatabase($"wardrobe_profile_tests_{Guid.NewGuid():N}")
            .Options;

        return new WardrobeDbContext(options, new StubCurrentUserService());
    }

    private static MemoryStream CreateValidPngStream(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Coral);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    [Fact]
    public async Task GetAndUpdateProfile_ReturnsFlagsAndUpdatesDisplayName()
    {
        await using var dbContext = CreateDbContext();
        var user = new User
        {
            Email = "user@example.com",
            DisplayName = "Old Name",
            PasswordHash = null,
            AvatarKey = null,
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sut = new ProfileService(
            dbContext,
            new SkiaImageProcessorService(),
            new InMemoryFileStorageService(),
            new PasswordHasherService());

        var initial = await sut.GetProfileAsync(user.Id);
        Assert.Equal("Old Name", initial.DisplayName);
        Assert.False(initial.HasAvatar);
        Assert.False(initial.HasPassword);

        var updated = await sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest("  New Name  "));
        Assert.Equal("New Name", updated.DisplayName);
    }

    [Fact]
    public async Task UpdateAvatar_And_DeleteAvatar_SavesSquareWebpAndDeletesOldFiles()
    {
        await using var dbContext = CreateDbContext();
        var storage = new InMemoryFileStorageService();
        var user = new User
        {
            Email = "avatar@example.com",
            DisplayName = "Avatar User",
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sut = new ProfileService(
            dbContext,
            new SkiaImageProcessorService(),
            storage,
            new PasswordHasherService());

        using var firstPng = CreateValidPngStream(800, 600);
        var profile1 = await sut.UpdateAvatarAsync(user.Id, firstPng, firstPng.Length);
        Assert.True(profile1.HasAvatar);
        var firstKey = await sut.GetAvatarKeyAsync(user.Id);
        Assert.NotNull(firstKey);
        Assert.True(storage.Files.ContainsKey(firstKey!));

        using var decoded = SKBitmap.Decode(storage.Files[firstKey!]);
        Assert.Equal(400, decoded.Width);
        Assert.Equal(400, decoded.Height);

        using var secondPng = CreateValidPngStream(500, 500);
        await sut.UpdateAvatarAsync(user.Id, secondPng, secondPng.Length);
        var secondKey = await sut.GetAvatarKeyAsync(user.Id);
        Assert.NotNull(secondKey);
        Assert.NotEqual(firstKey, secondKey);
        Assert.False(storage.Files.ContainsKey(firstKey!));
        Assert.True(storage.Files.ContainsKey(secondKey!));

        await sut.DeleteAvatarAsync(user.Id);
        Assert.Null(await sut.GetAvatarKeyAsync(user.Id));
        Assert.False(storage.Files.ContainsKey(secondKey!));
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidatesCurrentPassword_AllowsGoogleSetup_AndRevokesTokens()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new PasswordHasherService();

        var passwordUser = new User
        {
            Email = "pw@example.com",
            DisplayName = "PW User",
            PasswordHash = hasher.HashPassword("OldPass123"),
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        var googleUser = new User
        {
            Email = "google@example.com",
            DisplayName = "Google User",
            PasswordHash = null,
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.AddRange(passwordUser, googleUser);
        dbContext.RefreshTokens.AddRange(
            new RefreshToken { UserId = passwordUser.Id, TokenHash = "t1", ExpiresAt = DateTimeOffset.UtcNow.AddDays(10) },
            new RefreshToken { UserId = googleUser.Id, TokenHash = "t2", ExpiresAt = DateTimeOffset.UtcNow.AddDays(10) });
        await dbContext.SaveChangesAsync();

        var sut = new ProfileService(
            dbContext,
            new SkiaImageProcessorService(),
            new InMemoryFileStorageService(),
            hasher);

        // Wrong current password fails
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ChangePasswordAsync(passwordUser.Id, new ChangePasswordRequest("Wrong123", "NewPass123")));

        // Valid change updates hash and revokes refresh token
        await sut.ChangePasswordAsync(passwordUser.Id, new ChangePasswordRequest("OldPass123", "NewPass123"));
        Assert.True(hasher.VerifyPassword("NewPass123", passwordUser.PasswordHash!));
        var pwUserToken = await dbContext.RefreshTokens.SingleAsync(t => t.UserId == passwordUser.Id);
        Assert.NotNull(pwUserToken.RevokedAt);

        // Google user without password can set password without CurrentPassword
        await sut.ChangePasswordAsync(googleUser.Id, new ChangePasswordRequest(null, "GoogleNew123"));
        Assert.True(hasher.VerifyPassword("GoogleNew123", googleUser.PasswordHash!));
        var googleToken = await dbContext.RefreshTokens.SingleAsync(t => t.UserId == googleUser.Id);
        Assert.NotNull(googleToken.RevokedAt);
    }

    [Fact]
    public async Task DeleteAccountAsync_RemovesUserAndDeletesAllOwnedMediaOnly()
    {
        await using var dbContext = CreateDbContext();
        var storage = new InMemoryFileStorageService();

        var victim = new User
        {
            Email = "delete@example.com",
            DisplayName = "Victim",
            AvatarKey = "avatars/victim.webp",
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        var other = new User
        {
            Email = "keep@example.com",
            DisplayName = "Keep",
            AvatarKey = "avatars/keep.webp",
            EmailConfirmedAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.AddRange(victim, other);

        var victimItem = new Item
        {
            UserId = victim.Id,
            CategoryId = 3,
            Name = "Shirt",
            Style = ClothingStyle.Casual,
            PhotoKey = "items/victim/1.webp",
            ThumbKey = "items/victim/1_thumb.webp"
        };
        var otherItem = new Item
        {
            UserId = other.Id,
            CategoryId = 3,
            Name = "Other Shirt",
            Style = ClothingStyle.Casual,
            PhotoKey = "items/other/2.webp",
            ThumbKey = "items/other/2_thumb.webp"
        };
        dbContext.Items.AddRange(victimItem, otherItem);
        await dbContext.SaveChangesAsync();

        storage.Files["avatars/victim.webp"] = [1];
        storage.Files["items/victim/1.webp"] = [2];
        storage.Files["items/victim/1_thumb.webp"] = [3];
        storage.Files["avatars/keep.webp"] = [4];
        storage.Files["items/other/2.webp"] = [5];

        var sut = new ProfileService(
            dbContext,
            new SkiaImageProcessorService(),
            storage,
            new PasswordHasherService());

        await sut.DeleteAccountAsync(victim.Id);

        Assert.False(await dbContext.Users.AnyAsync(u => u.Id == victim.Id));
        Assert.True(await dbContext.Users.AnyAsync(u => u.Id == other.Id));
        Assert.False(storage.Files.ContainsKey("avatars/victim.webp"));
        Assert.False(storage.Files.ContainsKey("items/victim/1.webp"));
        Assert.False(storage.Files.ContainsKey("items/victim/1_thumb.webp"));
        Assert.True(storage.Files.ContainsKey("avatars/keep.webp"));
        Assert.True(storage.Files.ContainsKey("items/other/2.webp"));
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }

    private sealed class InMemoryFileStorageService : IFileStorageService
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public Task<string> SaveFileAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default)
        {
            Files[relativePath] = content;
            return Task.FromResult(relativePath);
        }

        public Task<Stream?> GetFileAsync(string fileKey, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(fileKey, out var bytes))
            {
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            }
            return Task.FromResult<Stream?>(null);
        }

        public Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default)
        {
            Files.Remove(fileKey);
            return Task.CompletedTask;
        }
    }
}
