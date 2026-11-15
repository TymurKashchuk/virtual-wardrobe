using Microsoft.Extensions.Configuration;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:BasePath"] ?? "storage";
        var fullBasePath = Path.GetFullPath(configuredPath);
        _basePath = Path.EndsInDirectorySeparator(fullBasePath)
            ? fullBasePath
            : fullBasePath + Path.DirectorySeparatorChar;

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
        var (safeRelativePath, fullPath) = ResolveSafePath(relativePath);

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
        var (_, fullPath) = ResolveSafePath(fileKey);

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
        var (_, fullPath) = ResolveSafePath(fileKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private (string SafeRelativePath, string FullPath) ResolveSafePath(string fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey) || fileKey.IndexOf('\0') >= 0)
        {
            throw new InvalidOperationException("Invalid file key.");
        }

        var safeRelativePath = fileKey.TrimStart('/', '\\').Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(safeRelativePath) || Path.IsPathRooted(safeRelativePath))
        {
            throw new InvalidOperationException("Access outside of storage directory is denied.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_basePath, safeRelativePath));
        EnsurePathIsInsideBase(fullPath);

        return (safeRelativePath, fullPath);
    }

    private void EnsurePathIsInsideBase(string fullPath)
    {
        if (!fullPath.StartsWith(_basePath, PathComparison))
        {
            throw new InvalidOperationException("Access outside of storage directory is denied.");
        }
    }
}
