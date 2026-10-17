using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

public class ItemRecoveryService : IItemRecoveryService
{
    private readonly string _recoverablesFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public ItemRecoveryService()
    {
        string appDataPath = AppPaths.DataDirectory;
        _recoverablesFilePath = Path.Combine(appDataPath, "Item_Recoverables.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task AddRecoverableAsync(CharacterItem item, Guid playerId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var recoverables = await ReadAllInternalAsync();
            recoverables.Add(CloneForRecovery(item, playerId));
            await WriteAllInternalAsync(recoverables);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<CharacterItem>> GetAllRecoverablesAsync()
    {
        await _fileLock.WaitAsync();
        try { return await ReadAllInternalAsync(); }
        finally { _fileLock.Release(); }
    }

    public async Task<List<CharacterItem>> GetRecoverablesForPlayerAsync(Guid playerId)
    {
        var all = await GetAllRecoverablesAsync();
        return all.Where(i => i.PlayerId == playerId).ToList();
    }

    public async Task<List<CharacterItem>> GetRecoverablesForSessionAsync(Guid playerId, Guid sessionId)
    {
        var all = await GetAllRecoverablesAsync();
        return all.Where(i => i.PlayerId == playerId && i.SessionId == sessionId).ToList();
    }

    public async Task PurgePlayerRecoverablesAsync(Guid playerId, Guid? keepSessionId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var recoverables = await ReadAllInternalAsync();
            int removed = recoverables.RemoveAll(i =>
                i.PlayerId == playerId && (keepSessionId == null || i.SessionId != keepSessionId));
            if (removed > 0) await WriteAllInternalAsync(recoverables);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<CharacterItem?> RecoverItemAsync(Guid instanceId, int newCharacterItemId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var recoverables = await ReadAllInternalAsync();
            var item = recoverables.FirstOrDefault(i => i.InstanceId == instanceId);
            if (item == null) return null;

            recoverables.Remove(item);
            await WriteAllInternalAsync(recoverables);

            item.Id = newCharacterItemId;
            item.IsDropped = false;
            item.DroppedAt = null;
            item.SessionId = null;   // back in the pack; a later drop gets its own session stamp

            return item;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private CharacterItem CloneForRecovery(CharacterItem source, Guid playerId)
    {
        return new CharacterItem
        {
            Id = source.Id,
            InstanceId = source.InstanceId,
            TemplateId = source.TemplateId,
            TemplateGuid = source.TemplateGuid,
            CharacterId = source.CharacterId,
            Quantity = source.Quantity,
            RemainingUses = source.RemainingUses,
            IsEquipped = false,
            IsIdentified = source.IsIdentified,
            CustomName = source.CustomName,
            PlayerNote = source.PlayerNote,
            WrittenValueInChips = source.WrittenValueInChips,
            AcquiredAt = source.AcquiredAt,
            LastModified = source.LastModified,
            DroppedAt = source.DroppedAt ?? DateTime.Now,
            IsDropped = true,
            SyncId = source.SyncId,
            PlayerId = playerId,
            Template = source.Template   // frozen snapshot, not re-resolved
        };
    }

    private async Task<List<CharacterItem>> ReadAllInternalAsync()
    {
        if (!File.Exists(_recoverablesFilePath)) return new List<CharacterItem>();
        string json = await File.ReadAllTextAsync(_recoverablesFilePath);
        if (string.IsNullOrWhiteSpace(json)) return new List<CharacterItem>();
        return JsonSerializer.Deserialize<List<CharacterItem>>(json, _jsonOptions) ?? new List<CharacterItem>();
    }

    private async Task WriteAllInternalAsync(List<CharacterItem> items)
    {
        string json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(_recoverablesFilePath, json);
    }

    public async Task AddRecoverableAsync(CharacterItem item, Guid playerId, Guid sessionId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var recoverables = await ReadAllInternalAsync();
            var entry = CloneForRecovery(item, playerId);
            entry.SessionId = sessionId;
            recoverables.Add(entry);
            await WriteAllInternalAsync(recoverables);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}