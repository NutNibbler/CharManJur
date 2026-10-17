using CharManJur.Models;

namespace CharManJur.Services;

public interface IAssetPackService
{
    Task<string> ExportPackAsync(
        string packName, string? author,
        List<Guid> itemGuidsToInclude, List<Guid> backgroundGuidsToInclude, List<Guid> familiarGuidsToInclude,
        List<Guid> raceGuidsToInclude, List<Guid> hinderanceGuidsToInclude,
        List<Guid> blueprintGuidsToInclude, List<Guid> spellGuidsToInclude, List<Guid> quipGuidsToInclude, List<Guid> techniqueGuidsToInclude,
        bool moveItems, List<Guid>? traitGuidsToInclude = null);
    Task<(bool Success, string Message)> ImportPackAsync(string filePath);
    Task<List<InstalledPackEntry>> GetInstalledPacksAsync();
    Task SetPackLoadedAsync(string packId, bool isLoaded);
    Task<(bool Success, string Message)> DeletePackAsync(string packId);
    Task<(bool Success, string Message)> CopyItemsToPackAsync(
        string targetPackId,
        List<Guid> itemGuidsToCopy, List<Guid> backgroundGuidsToCopy, List<Guid> familiarGuidsToCopy,
        List<Guid> raceGuidsToCopy, List<Guid> hinderanceGuidsToCopy,
        List<Guid> blueprintGuidsToCopy, List<Guid> spellGuidsToCopy, List<Guid> quipGuidsToCopy, List<Guid> techniqueGuidsToCopy,
        List<Guid>? traitGuidsToCopy = null);
    Task<(bool Success, string Message)> UpdatePackAsync(string packId, string name, string? description, string? details, PackAssetSyncMode syncMode);
}