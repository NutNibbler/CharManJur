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
        string appDataPath = AppPaths.DataDirectory;
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

    // Set by the character service when a session starts; every entry written is stamped with it.
    public Guid? CurrentSessionId { get; set; }
    public event Action<PlayerActionLogEntry>? EntryLogged;

    public Task LogItemDroppedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid) =>
    AppendEntryAsync(new PlayerActionLogEntry
    {
        ActionType = PlayerActionType.ItemDropped,
        PlayerName = playerName,
        PlayerId = playerId,
        CharacterName = characterName,
        CharacterGuid = characterGuid,
        ItemName = item.DisplayName,
        ItemId = item.TemplateId,
        Summary = item.Quantity > 1
            ? $"Dropped {item.Quantity}x {item.DisplayName}"
            : $"Dropped {item.DisplayName}"
    });

    public Task LogItemBestowedAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid) =>
        AppendEntryAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemBestowed,
            PlayerName = playerName,
            PlayerId = playerId,
            CharacterName = characterName,
            CharacterGuid = characterGuid,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId,
            Summary = $"Bestowed {item.DisplayName}"
        });

    public Task LogItemRecoveredAsync(CharacterItem item, string playerName, Guid playerId, string characterName, Guid characterGuid) =>
        AppendEntryAsync(new PlayerActionLogEntry
        {
            ActionType = PlayerActionType.ItemRecovered,
            PlayerName = playerName,
            PlayerId = playerId,
            CharacterName = characterName,
            CharacterGuid = characterGuid,
            ItemName = item.DisplayName,
            ItemId = item.TemplateId,
            Summary = $"Recovered {item.DisplayName}"
        });

    public Task LogEventAsync(PlayerActionLogEntry entry) => AppendEntryAsync(entry);

    private async Task AppendEntryAsync(PlayerActionLogEntry entry)
    {
        entry.SessionId ??= CurrentSessionId;
        bool written = false;

        await _fileLock.WaitAsync();
        try
        {
            var entries = await ReadAllEntriesInternalAsync();
            entries.Add(entry);
            string json = JsonSerializer.Serialize(entries, _jsonOptions);
            await File.WriteAllTextAsync(_logFilePath, json);
            written = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlayerActionLog write error: {ex.Message}");
        }
        finally
        {
            _fileLock.Release();
        }

        // Outside the lock, so a slow listener can never hold up logging.
        if (written)
        {
            try { EntryLogged?.Invoke(entry); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"EntryLogged listener error: {ex.Message}"); }
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