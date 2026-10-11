namespace FamilyBudget.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string originalFileName, string subFolder);
    Task<bool> DeleteFileAsync(string relativePath);
    bool IsAllowedFileType(string fileName);
}
