using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace MultiverseCRM.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _log;

    private static readonly string[] AllowedImageTypes =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private static readonly string[] AllowedDocTypes =
        { ".pdf", ".docx", ".doc", ".xlsx", ".xls" };

    private const long MaxImageSize = 5 * 1024 * 1024;      // 5 MB
    private const long MaxDocSize   = 20 * 1024 * 1024;     // 20 MB

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> log)
    {
        _env = env;
        _log = log;
    }

    public async Task<string> SaveProjectImageAsync(int projectId, IFormFile file, CancellationToken ct = default)
    {
        return await SaveAsync($"uploads/projects/{projectId}/images", file, ct);
    }

    public async Task<string> SaveProjectDocumentAsync(int projectId, IFormFile file, CancellationToken ct = default)
    {
        return await SaveAsync($"uploads/projects/{projectId}/documents", file, ct);
    }

    private async Task<string> SaveAsync(string relativeFolder, IFormFile file, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var relativePath = $"{relativeFolder}/{uniqueName}";
        var physicalRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var physicalPath = Path.Combine(physicalRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var dir = Path.GetDirectoryName(physicalPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

        await using var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await file.CopyToAsync(stream, ct);

        return relativePath;
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

        try
        {
            var physicalRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var physicalPath = Path.Combine(physicalRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _log.LogInformation("Deleted file: {Path}", physicalPath);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to delete file: {Path}", relativePath);
        }

        return Task.CompletedTask;
    }

    public bool IsValidImage(IFormFile file, out string? error)
    {
        error = null;
        if (file == null || file.Length == 0)
        {
            error = "File is empty.";
            return false;
        }
        if (file.Length > MaxImageSize)
        {
            error = $"Image exceeds {MaxImageSize / (1024 * 1024)} MB limit.";
            return false;
        }
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageTypes.Contains(ext))
        {
            error = $"Invalid image type. Allowed: {string.Join(", ", AllowedImageTypes)}";
            return false;
        }
        return true;
    }

    public bool IsValidDocument(IFormFile file, out string? error)
    {
        error = null;
        if (file == null || file.Length == 0)
        {
            error = "File is empty.";
            return false;
        }
        if (file.Length > MaxDocSize)
        {
            error = $"Document exceeds {MaxDocSize / (1024 * 1024)} MB limit.";
            return false;
        }
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedDocTypes.Contains(ext))
        {
            error = $"Invalid document type. Allowed: {string.Join(", ", AllowedDocTypes)}";
            return false;
        }
        return true;
    }
}