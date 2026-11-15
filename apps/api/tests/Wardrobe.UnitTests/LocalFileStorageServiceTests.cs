using Microsoft.Extensions.Configuration;
using Wardrobe.Infrastructure.Services;

namespace Wardrobe.UnitTests;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _storagePath;
    private readonly string _siblingPath;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"wardrobe_storage_tests_{Guid.NewGuid():N}");
        _storagePath = Path.Combine(_tempRoot, "storage");
        _siblingPath = Path.Combine(_tempRoot, "storage_evil");
        Directory.CreateDirectory(_siblingPath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:BasePath"] = _storagePath
            })
            .Build();

        _sut = new LocalFileStorageService(config);
    }

    [Fact]
    public async Task SaveGetAndDelete_InsideBaseDirectory_WorksCorrectly()
    {
        var content = new byte[] { 1, 2, 3, 4 };
        var key = await _sut.SaveFileAsync("items/user1/test.webp", content);

        Assert.Equal("items/user1/test.webp", key);

        await using (var stream = await _sut.GetFileAsync(key))
        {
            Assert.NotNull(stream);
            using var ms = new MemoryStream();
            await stream!.CopyToAsync(ms);
            Assert.Equal(content, ms.ToArray());
        }

        await _sut.DeleteFileAsync(key);
        Assert.Null(await _sut.GetFileAsync(key));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData(@"..\secret.txt")]
    [InlineData("items/../../secret.txt")]
    [InlineData("../storage_evil/secret.txt")]
    [InlineData(@"..\storage_evil\secret.txt")]
    [InlineData("/etc/passwd")]
    [InlineData(@"\Windows\win.ini")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("items/\0bad.webp")]
    public async Task Operations_WithTraversalOrInvalidKey_ThrowInvalidOperationException(string badKey)
    {
        var evilFile = Path.Combine(_siblingPath, "secret.txt");
        await File.WriteAllTextAsync(evilFile, "sensitive");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SaveFileAsync(badKey, [1, 2]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetFileAsync(badKey));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteFileAsync(badKey));

        Assert.True(File.Exists(evilFile));
    }

    [Fact]
    public async Task Operations_WithAbsolutePath_ThrowInvalidOperationException()
    {
        var absolutePath = Path.Combine(_siblingPath, "abs.txt");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetFileAsync(absolutePath));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SaveFileAsync(absolutePath, [1, 2]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteFileAsync(absolutePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
