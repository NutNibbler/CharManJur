using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

public class PlayerActionLogService : IPlayerActionLogService
{
    private readonly string _logFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public PlayerActionLogService()
    {
        string appDataPath = FileSystem.AppDataDirectory;
        string logDirectory = Path.Combine(appDataPath, "Logs");

        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        _logFilePath = Path.Combine(logDirectory, "PlayerActionLog.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId) =>
        AppendEntryAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemDropped,
            PlayerName = playerName,
            PlayerId = playerId,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId
        });

    public Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId) =>
        AppendEntryAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemBestowed,
            PlayerName = playerName,
            PlayerId = playerId,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId
        });

    private async Task AppendEntryAsync(PlayerActionLogEntry entry)
    {
        await _fileLock.WaitAsync();
        try
        {
            var entries = await ReadAllEntriesInternalAsync();
            entries.Add(entry);
            string json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(_logFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlayerActionLog write error: {ex.Message}");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<PlayerActionLogEntry>> GetAllEntriesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            return await ReadAllEntriesInternalAsync();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<PlayerActionLogEntry>> ReadAllEntriesInternalAsync()
    {
        if (!File.Exists(_logFilePath)) return new List<PlayerActionLogEntry>();

        string json = await File.ReadAllTextAsync(_logFilePath);
        if (string.IsNullOrWhiteSpace(json)) return new List<PlayerActionLogEntry>();

        return JsonSerializer.Deserialize<List<PlayerActionLogEntry>>(json, _jsonOptions)
               ?? new List<PlayerActionLogEntry>();
    }
}