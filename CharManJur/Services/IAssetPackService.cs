using CharManJur.Models;

namespace CharManJur.Services;

public interface IAssetPackService
{
    Task<string> ExportPackAsync(string packName, string? author, List<Guid> itemGuidsToInclude, bool moveItems);
    Task<(bool Success, string Message)> ImportPackAsync(string filePath);
    Task<List<InstalledPackEntry>> GetInstalledPacksAsync();
    Task SetPackLoadedAsync(string packId, bool isLoaded);
    Task<(bool Success, string Message)> DeletePackAsync(string packId);
    Task<(bool Success, string Message)> CopyItemsToPackAsync(string targetPackId, List<Guid> itemGuidsToCopy);
    Task<(bool Success, string Message)> UpdatePackAsync(string packId, string name, string? description, string? details, PackAssetSyncMode syncMode);

}