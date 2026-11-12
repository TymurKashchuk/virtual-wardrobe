namespace Wardrobe.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string fileKey, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileKey, CancellationToken cancellationToken = default);
}
