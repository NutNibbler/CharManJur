using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

public class AssetPackService : IAssetPackService
{
    private readonly IItemDataService _itemDataService;
    private readonly ICustomItemStorageService _customItemStorageService;
    private readonly string _packsDirectory;
    private readonly string _indexFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AssetPackService(IItemDataService itemDataService, ICustomItemStorageService customItemStorageService)
    {
        _itemDataService = itemDataService;
        _customItemStorageService = customItemStorageService;

        string appDataPath = FileSystem.AppDataDirectory;
        _packsDirectory = Path.Combine(appDataPath, "AssetPacks");
        _indexFilePath = Path.Combine(_packsDirectory, "InstalledPacks.json");

        if (!Directory.Exists(_packsDirectory))
        {
            Directory.CreateDirectory(_packsDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<string> ExportPackAsync(string packName, string? author, List<Guid> itemGuidsToInclude, bool moveItems)
    {
        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        var itemsToExport = allCustomItems.Where(i => itemGuidsToInclude.Contains(i.Guid)).ToList();

        var pack = new AssetPack
        {
            Name = packName,
            Author = author,
            Items = itemsToExport
        };

        if (moveItems)
        {
            foreach (var item in itemsToExport)
            {
                item.SourcePackId = pack.PackId;
            }
        }

        string safeName = string.Join("_", packName.Split(Path.GetInvalidFileNameChars()));
        string fileName = $"{safeName}_{pack.PackId.Substring(0, 8)}.json";
        string filePath = Path.Combine(_packsDirectory, fileName);

        string json = JsonSerializer.Serialize(pack, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        var index = await LoadIndexAsync();
        if (!index.Any(p => p.PackId == pack.PackId))
        {
            index.Add(new InstalledPackEntry
            {
                PackId = pack.PackId,
                Name = pack.Name,
                FileName = fileName,
                IsLoaded = false // a freshly saved pack starts unloaded — you load it explicitly when you want it active
            });
            await SaveIndexAsync(index);
        }

        if (moveItems)
        {
            var storedItems = await _customItemStorageService.LoadCustomItemsAsync();
            foreach (var guid in itemGuidsToInclude)
            {
                var stored = storedItems.FirstOrDefault(i => i.Guid == guid);
                if (stored != null)
                {
                    stored.SourcePackId = pack.PackId;
                    stored.IsLoaded = false;
                }
            }
            await _customItemStorageService.SaveCustomItemsAsync(storedItems);

            // Match the live in-memory copies to the same state immediately, not just on next load
            foreach (var item in itemsToExport)
            {
                item.IsLoaded = false;
                item.Id = 0;
            }
        }

        return filePath;
    }

    public async Task<(bool Success, string Message)> ImportPackAsync(string filePath)
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            return (false, $"Could not read file: {ex.Message}");
        }

        AssetPack? pack;
        try
        {
            pack = JsonSerializer.Deserialize<AssetPack>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            return (false, $"Invalid pack file: {ex.Message}");
        }

        if (pack == null || pack.Items.Count == 0)
        {
            return (false, "Pack file contained no items.");
        }

        var existingCustomItems = await _customItemStorageService.LoadCustomItemsAsync();
        int overwritten = 0;
        int added = 0;

        foreach (var incomingItem in pack.Items)
        {
            incomingItem.SourcePackId = pack.PackId;
            incomingItem.IsLoaded = true;
            incomingItem.IsPlayerCreated = true;

            var existing = existingCustomItems.FirstOrDefault(i => i.Guid == incomingItem.Guid);
            if (existing != null)
            {
                existingCustomItems.Remove(existing);
                overwritten++;
            }
            else
            {
                added++;
            }

            existingCustomItems.Add(incomingItem);
        }

        await _customItemStorageService.SaveCustomItemsAsync(existingCustomItems);

        string safeName = string.Join("_", pack.Name.Split(Path.GetInvalidFileNameChars()));
        string fileName = $"{safeName}_{pack.PackId.Substring(0, 8)}.json";
        string destPath = Path.Combine(_packsDirectory, fileName);
        if (Path.GetFullPath(filePath) != Path.GetFullPath(destPath))
        {
            File.Copy(filePath, destPath, overwrite: true);
        }

        var index = await LoadIndexAsync();
        var existingEntry = index.FirstOrDefault(p => p.PackId == pack.PackId);
        if (existingEntry != null)
        {
            existingEntry.IsLoaded = true;
        }
        else
        {
            index.Add(new InstalledPackEntry
            {
                PackId = pack.PackId,
                Name = pack.Name,
                FileName = fileName,
                IsLoaded = true
            });
        }
        await SaveIndexAsync(index);

        string message = overwritten > 0
            ? $"Imported '{pack.Name}': {added} new item(s), {overwritten} existing item(s) overwritten with the pack's version."
            : $"Imported '{pack.Name}': {added} new item(s).";

        return (true, message);
    }

    public async Task<List<InstalledPackEntry>> GetInstalledPacksAsync()
    {
        return await LoadIndexAsync();
    }

    public async Task SetPackLoadedAsync(string packId, bool isLoaded)
    {
        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        var packItems = allCustomItems.Where(i => i.SourcePackId == packId).ToList();

        foreach (var item in packItems)
        {
            if (isLoaded)
                await _itemDataService.LoadItemAsync(item.Guid);
            else
                await _itemDataService.UnloadItemAsync(item.Guid);
        }

        var index = await LoadIndexAsync();
        var entry = index.FirstOrDefault(p => p.PackId == packId);
        if (entry != null)
        {
            entry.IsLoaded = isLoaded;
            await SaveIndexAsync(index);
        }
    }

    public async Task<(bool Success, string Message)> DeletePackAsync(string packId)
    {
        if (packId == "Local")
        {
            return (false, "The Local pack can't be deleted.");
        }

        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        var packItems = allCustomItems.Where(i => i.SourcePackId == packId).ToList();

        foreach (var item in packItems)
        {
            await _itemDataService.DeleteItemAsync(item.Guid);
        }

        var index = await LoadIndexAsync();
        var entry = index.FirstOrDefault(p => p.PackId == packId);
        if (entry != null)
        {
            string packFilePath = Path.Combine(_packsDirectory, entry.FileName);
            if (File.Exists(packFilePath))
            {
                File.Delete(packFilePath);
            }

            index.Remove(entry);
            await SaveIndexAsync(index);
        }

        return (true, "Pack deleted.");
    }

    public async Task<(bool Success, string Message)> CopyItemsToPackAsync(string targetPackId, List<Guid> itemGuidsToCopy)
    {
        var index = await LoadIndexAsync();
        var targetEntry = index.FirstOrDefault(p => p.PackId == targetPackId);
        if (targetEntry == null)
        {
            return (false, "Target pack not found.");
        }

        var allCustomItems = await _customItemStorageService.LoadCustomItemsAsync();
        var itemsToCopy = allCustomItems.Where(i => itemGuidsToCopy.Contains(i.Guid)).ToList();

        if (itemsToCopy.Count == 0)
        {
            return (false, "No matching items found to copy.");
        }

        List<Item> targetPackItems = new();
        string? targetFilePath = null;

        if (!string.IsNullOrEmpty(targetEntry.FileName))
        {
            targetFilePath = Path.Combine(_packsDirectory, targetEntry.FileName);
            if (File.Exists(targetFilePath))
            {
                string existingJson = await File.ReadAllTextAsync(targetFilePath);
                var existingPack = JsonSerializer.Deserialize<AssetPack>(existingJson, _jsonOptions);
                targetPackItems = existingPack?.Items ?? new List<Item>();
            }
        }

        int copied = 0;
        foreach (var original in itemsToCopy)
        {
            var duplicate = new Item
            {
                Guid = Guid.NewGuid(),
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                Name = original.Name,
                Category = original.Category,
                BaseDescription = original.BaseDescription,
                Size = original.Size,
                ValueInChips = original.ValueInChips,
                Rarity = original.Rarity,
                Uses = original.Uses,
                QtyLimit = original.QtyLimit,
                IsStackable = original.IsStackable,
                WeaponCategory = original.WeaponCategory,
                WeaponDamage = original.WeaponDamage,
                WeaponEffects = original.WeaponEffects != null ? new List<WeaponEffectType>(original.WeaponEffects) : null,
                WeaponSpeed = original.WeaponSpeed,
                ArmorType = original.ArmorType,
                ArmorValue = original.ArmorValue
            };

            allCustomItems.Add(duplicate);
            if (targetPackId != "Local")
            {
                targetPackItems.Add(duplicate);
            }
            copied++;
        }

        await _customItemStorageService.SaveCustomItemsAsync(allCustomItems);

        if (targetFilePath != null)
        {
            var updatedPack = new AssetPack { PackId = targetPackId, Name = targetEntry.Name, Items = targetPackItems };
            string json = JsonSerializer.Serialize(updatedPack, _jsonOptions);
            await File.WriteAllTextAsync(targetFilePath, json);
        }

        return (true, $"Copied {copied} item(s) to '{targetEntry.Name}'.");
    }

    private async Task<List<InstalledPackEntry>> LoadIndexAsync()
    {
        if (!File.Exists(_indexFilePath)) return new List<InstalledPackEntry>();

        try
        {
            string json = await File.ReadAllTextAsync(_indexFilePath);
            return JsonSerializer.Deserialize<List<InstalledPackEntry>>(json, _jsonOptions) ?? new();
        }
        catch
        {
            return new List<InstalledPackEntry>();
        }
    }

    private async Task SaveIndexAsync(List<InstalledPackEntry> index)
    {
        string json = JsonSerializer.Serialize(index, _jsonOptions);
        await File.WriteAllTextAsync(_indexFilePath, json);
    }

    public async Task<(bool Success, string Message)> UpdatePackAsync(string packId, string name, string? description, string? details, PackAssetSyncMode syncMode)
    {
        var index = await LoadIndexAsync();
        var entry = index.FirstOrDefault(p => p.PackId == packId);
        if (entry == null)
        {
            return (false, "Pack not found.");
        }

        bool isLocal = string.IsNullOrEmpty(entry.FileName);
        if (isLocal && syncMode != PackAssetSyncMode.None)
        {
            return (false, "The Local pack can't be asset-synced this way.");
        }

        entry.Name = name;
        entry.Description = description;
        entry.Details = details;

        string message = "Pack details updated.";

        if (syncMode != PackAssetSyncMode.None)
        {
            var storedItems = await _customItemStorageService.LoadCustomItemsAsync();
            List<Item> updatedPackItems;

            if (syncMode == PackAssetSyncMode.OverwriteOnly)
            {
                updatedPackItems = storedItems.Where(i => i.SourcePackId == packId).ToList();
                message = $"'{name}' updated with the current state of its {updatedPackItems.Count} item(s).";
            }
            else
            {
                var loadedItems = storedItems.Where(i => i.IsLoaded).ToList();
                int moved = 0;

                foreach (var item in loadedItems)
                {
                    if (item.SourcePackId != packId)
                    {
                        item.SourcePackId = packId;
                        moved++;
                    }
                }

                updatedPackItems = loadedItems;
                message = $"'{name}' updated: {updatedPackItems.Count} loaded item(s) now in the pack ({moved} newly moved in).";

                foreach (var item in loadedItems)
                {
                    var live = await _itemDataService.GetItemByGuidAsync(item.Guid);
                    if (live != null)
                    {
                        live.SourcePackId = item.SourcePackId;
                    }
                }
            }

            await _customItemStorageService.SaveCustomItemsAsync(storedItems);

            var updatedPack = new AssetPack { PackId = packId, Name = name, Items = updatedPackItems };
            string packFilePath = Path.Combine(_packsDirectory, entry.FileName);
            string json = JsonSerializer.Serialize(updatedPack, _jsonOptions);
            await File.WriteAllTextAsync(packFilePath, json);
        }

        await SaveIndexAsync(index);

        return (true, message);
    }
}