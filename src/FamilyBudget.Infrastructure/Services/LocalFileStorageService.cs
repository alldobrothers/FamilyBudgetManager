using FamilyBudget.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyBudget.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalFileStorageService> _logger;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".txt", ".csv"
    };

    public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public bool IsAllowedFileType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(ext);
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder)
    {
        if (!IsAllowedFileType(originalFileName))
        {
            throw new InvalidOperationException($"File type '{Path.GetExtension(originalFileName)}' is not permitted.");
        }

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{ext}";

        // Store inside wwwroot/uploads/{subFolder}
        var rootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var folderPath = Path.Combine(rootPath, "uploads", subFolder);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, safeFileName);
        using (var outputStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(outputStream);
        }

        _logger.LogInformation("Saved uploaded file '{FileName}' as '{SafeName}' in '{Folder}'", originalFileName, safeFileName, subFolder);
        return $"/uploads/{subFolder}/{safeFileName}";
    }

    public Task<bool> DeleteFileAsync(string relativePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return Task.FromResult(false);

            var rootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var cleanRelative = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(rootPath, cleanRelative);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file at '{Path}'", fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at '{Path}'", relativePath);
            return Task.FromResult(false);
        }
    }
}
