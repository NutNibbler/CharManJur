using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

// Plain JSON on disk, one folder per campaign:
//   <data>/Campaigns/<campaign>/campaign.json                     the campaign and its running session
//   <data>/Campaigns/<campaign>/Snapshots/<character guid>.json   character copies saved when the session closes
//   <data>/Campaigns/<campaign>/events.json, events.archive.jsonl the GM's event log
//   <data>/Campaigns/<campaign>/Pad, Maps, Symbols                the GM's own scratch pad, maps and symbol library
public sealed class CampaignStore : ICampaignStore
{
    private readonly string _root;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions FileOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    // Used for hashing: compact, and every character goes through the same round trip first.
    private static readonly JsonSerializerOptions HashOptions = new() { PropertyNameCaseInsensitive = true };

    public CampaignStore()
    {
        _root = Path.Combine(AppPaths.DataDirectory, "Campaigns");
        Directory.CreateDirectory(_root);
    }

    // ===== PATHS =====
    private string CampaignDir(Guid id) => Path.Combine(_root, id.ToString("N"));
    private string CampaignFile(Guid id) => Path.Combine(CampaignDir(id), "campaign.json");
    private string SnapshotFile(Guid campaignId, Guid characterGuid) => Path.Combine(CampaignDir(campaignId), "Snapshots", characterGuid.ToString("N") + ".json");

    public string CampaignFolder(Guid campaignId) => CampaignDir(campaignId);
    public string PadFolder(Guid campaignId) => Path.Combine(CampaignDir(campaignId), "Pad");
    public string MapsFolder(Guid campaignId) => Path.Combine(CampaignDir(campaignId), "Maps");
    public string SymbolsFolder(Guid campaignId) => Path.Combine(CampaignDir(campaignId), "Symbols");

    // ===== FILE HELPERS =====
    // Written to a temp file first, so a crash mid-save can't leave a half-written campaign behind.
    private static async Task WriteJsonAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(value, FileOptions));
        File.Move(temp, path, overwrite: true);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), FileOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Campaign read error ({path}): {ex.Message}");
            return null;
        }
    }

    private async Task<T> LockedAsync<T>(Func<Task<T>> work)
    {
        await _lock.WaitAsync();
        try { return await work(); }
        finally { _lock.Release(); }
    }

    // Campaigns used to have separate sessions. That layer is gone, and its old data is dropped.
    private static void DropLegacySessions(string campaignDir)
    {
        try
        {
            string old = Path.Combine(campaignDir, "Sessions");
            if (Directory.Exists(old)) Directory.Delete(old, recursive: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Couldn't remove old sessions: {ex.Message}");
        }
    }

    // ===== CAMPAIGNS =====
    public Task<List<Campaign>> GetCampaignsAsync() => LockedAsync(async () =>
    {
        var result = new List<Campaign>();
        foreach (var dir in Directory.GetDirectories(_root))
        {
            var campaign = await ReadJsonAsync<Campaign>(Path.Combine(dir, "campaign.json"));
            if (campaign == null) continue;

            DropLegacySessions(dir);
            campaign.Normalize();
            result.Add(campaign);
        }
        return result.OrderByDescending(c => c.LastOpenedUtc).ToList();
    });

    public Task<Campaign?> LoadCampaignAsync(Guid campaignId) => LockedAsync<Campaign?>(async () =>
    {
        var campaign = await ReadJsonAsync<Campaign>(CampaignFile(campaignId));
        if (campaign == null) return null;

        DropLegacySessions(CampaignDir(campaignId));
        campaign.Normalize();
        return campaign;
    });

    public Task<string?> SaveCampaignAsync(Campaign campaign) => LockedAsync<string?>(async () =>
    {
        campaign.Normalize();
        string? error = campaign.Validate();
        if (error != null) return error;

        await WriteJsonAsync(CampaignFile(campaign.Id), campaign);
        return null;
    });

    // The running session changes often (time, password, policy): saved as it is, without the creator's checks.
    public Task SaveStateAsync(Campaign campaign) => LockedAsync(async () =>
    {
        await WriteJsonAsync(CampaignFile(campaign.Id), campaign);
        return true;
    });

    public Task<bool> DeleteCampaignAsync(Guid campaignId) => LockedAsync(() =>
    {
        string dir = CampaignDir(campaignId);
        if (!Directory.Exists(dir)) return Task.FromResult(false);
        Directory.Delete(dir, recursive: true);   // snapshots, event log, pad and maps go with it
        return Task.FromResult(true);
    });

    // ===== SNAPSHOTS (cloned when the session closes) =====
    public Task SnapshotCharactersAsync(Campaign campaign, IEnumerable<CharacterSaveData> characters)
        => LockedAsync(async () =>
        {
            foreach (var character in characters)
            {
                var clone = Normalize(character);   // a deep copy, so the saved clone never shares state with the live character
                await WriteJsonAsync(SnapshotFile(campaign.Id, clone.CharacterGuid), clone);

                campaign.Characters.RemoveAll(r => r.CharacterGuid == clone.CharacterGuid);
                campaign.Characters.Add(new SessionCharacterRecord
                {
                    CharacterGuid = clone.CharacterGuid,
                    PlayerId = clone.PlayerId,
                    PlayerName = clone.PlayerName,
                    CharacterName = clone.CharacterName,
                    SnapshotHash = HashData(clone.Data),
                    SnapshotUtc = DateTime.UtcNow
                });
            }

            campaign.LastClosedUtc = DateTime.UtcNow;
            await WriteJsonAsync(CampaignFile(campaign.Id), campaign);
            return true;
        });

    public Task<CharacterSaveData?> GetSnapshotAsync(Campaign campaign, Guid characterGuid)
        => LockedAsync(() => ReadJsonAsync<CharacterSaveData>(SnapshotFile(campaign.Id, characterGuid)));

    // Compares a character about to rejoin against what was saved when the session last closed.
    // The hash answers "changed or not"; the section list tells the GM what changed.
    public async Task<TamperReport> ValidateCharacterAsync(Campaign campaign, CharacterSaveData current)
    {
        var record = campaign.Characters.FirstOrDefault(r => r.CharacterGuid == current.CharacterGuid);
        if (record == null) return new TamperReport { HadSnapshot = false, Matches = true };

        if (HashData(current.Data) == record.SnapshotHash)
            return new TamperReport { HadSnapshot = true, Matches = true };

        var changed = new List<string>();
        var snapshot = await GetSnapshotAsync(campaign, current.CharacterGuid);
        if (snapshot != null)
        {
            var before = JsonSerializer.SerializeToNode(Normalize(snapshot).Data, HashOptions)!.AsObject();
            var after = JsonSerializer.SerializeToNode(Normalize(current).Data, HashOptions)!.AsObject();

            foreach (string key in before.Select(p => p.Key).Union(after.Select(p => p.Key)))
            {
                if (before[key]?.ToJsonString() != after[key]?.ToJsonString()) changed.Add(key);
            }
        }

        return new TamperReport { HadSnapshot = true, Matches = false, ChangedSections = changed };
    }

    // ===== HASHING =====
    private static CharacterSaveData Normalize(CharacterSaveData data)
        => JsonSerializer.Deserialize<CharacterSaveData>(JsonSerializer.Serialize(data, HashOptions), HashOptions)!;

    // Only the character's Data is hashed: the save wrapper's LastSaved changes on every autosave.
    private static string HashData(CharacterData data)
    {
        var roundTripped = JsonSerializer.Deserialize<CharacterData>(JsonSerializer.Serialize(data, HashOptions), HashOptions)!;
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(roundTripped, HashOptions));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}