namespace MultiverseCRM.Services;

public interface IFileStorageService
{
    Task<string> SaveProjectImageAsync(int projectId, IFormFile file, CancellationToken ct = default);
    Task<string> SaveProjectDocumentAsync(int projectId, IFormFile file, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
    bool IsValidImage(IFormFile file, out string? error);
    bool IsValidDocument(IFormFile file, out string? error);
}