using Microsoft.Extensions.Configuration;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:BasePath"] ?? "storage";
        _basePath = Path.GetFullPath(configuredPath);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(
        string relativePath, 
        byte[] content, 
        CancellationToken cancellationToken = default)
    {
        var safeRelativePath = relativePath.TrimStart('/', '\\').Replace('\\', '/');
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, safeRelativePath));

        EnsurePathIsInsideBase(fullPath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return safeRelativePath;
    }

    public Task<Stream?> GetFileAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var safeRelativePath = fileKey.TrimStart('/', '\\').Replace('\\', '/');
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, safeRelativePath));

        EnsurePathIsInsideBase(fullPath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            fullPath, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize: 4096, 
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var safeRelativePath = fileKey.TrimStart('/', '\\').Replace('\\', '/');
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, safeRelativePath));

        EnsurePathIsInsideBase(fullPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private void EnsurePathIsInsideBase(string fullPath)
    {
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Access outside of storage directory is denied.");
        }
    }
}
