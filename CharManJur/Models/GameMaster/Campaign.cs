using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace CharManJur.Models;

// What happens to the custom assets a connecting player brings. Chosen per campaign and changeable at any time;
// a change only affects players as they join. New values go at the end: the policy is saved as a number.
public enum CustomAssetPolicy { GmOnly, MergeAll, PromptMerge, CharacterOnly }

public static class AssetPolicyInfo
{
    public static readonly string[] Labels = { "GM Only", "Merge All", "Prompt Merge", "Character Only" };

    public static readonly string[] Descriptions =
    {
        "No custom assets are allowed. A player whose character carries any is told so and can't join.",
        "Nothing is asked. All of the player's custom assets are merged into the campaign's asset pack when they join.",
        "You see a list of the player's custom assets and approve each one before it is merged. Refusing a custom background disconnects the player.",
        "Assets the player's character uses are accepted automatically. Any other custom assets the player has loaded are rejected."
    };

    public static string DescriptionFor(CustomAssetPolicy policy)
    {
        int i = (int)policy;
        return i >= 0 && i < Descriptions.Length ? Descriptions[i] : string.Empty;
    }
}

// What was saved for one player character when the session last closed.
public class SessionCharacterRecord
{
    public Guid CharacterGuid { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public DateTime SnapshotUtc { get; set; }
}

// A campaign is the data framework and the one running session: its calendar and clock, NPC fields, factions,
// asset policy, password and the characters saved when the session last closed. Maps, the GM's pad and the
// symbol library live in the campaign's folder.
public class Campaign
{
    public int SchemaVersion { get; set; } = 3;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastOpenedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastClosedUtc { get; set; }

    public CalendarConfig Calendar { get; set; } = new();
    public List<NpcFieldDefinition> NpcFields { get; set; } = NpcFieldDefaults.Create();
    public List<Faction> Factions { get; set; } = new();

    // ===== THE RUNNING SESSION =====
    // In-game time. Day counts from 1; Hour is 0-based within the day.
    public int Day { get; set; } = 1;
    public int Hour { get; set; }

    // Never saved: a campaign is closed whenever it is loaded, and a closed session blocks joining.
    [JsonIgnore] public bool IsOpen { get; set; }

    public CustomAssetPolicy AssetPolicy { get; set; } = CustomAssetPolicy.PromptMerge;
    public string AssetPackName { get; set; } = string.Empty;

    // Optional password. Only a salted hash is kept.
    public string? PasswordSalt { get; set; }
    public string? PasswordHash { get; set; }
    [JsonIgnore] public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    public List<SessionCharacterRecord> Characters { get; set; } = new();

    // Pass a Day moves to the next day and back to the earliest hour.
    public void PassDay()
    {
        Day++;
        Hour = 0;
    }

    public void SetHour(int hour) => Hour = Math.Clamp(hour, 0, Math.Max(1, Calendar.HoursPerDay) - 1);

    // The password hash uses this many PBKDF2 rounds. The session security layer reuses the same value, so it must not change casually.
    public const int PasswordIterations = 100_000;

    public void SetPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            PasswordSalt = null;
            PasswordHash = null;
            return;
        }

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        PasswordSalt = Convert.ToBase64String(salt);
        PasswordHash = Convert.ToBase64String(Derive(password, salt));
    }

    // A campaign with no password accepts anyone.
    public bool VerifyPassword(string? attempt)
    {
        if (!HasPassword) return true;
        if (string.IsNullOrEmpty(attempt) || PasswordSalt == null) return false;

        byte[] expected = Convert.FromBase64String(PasswordHash!);
        byte[] actual = Derive(attempt, Convert.FromBase64String(PasswordSalt));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static byte[] Derive(string password, byte[] salt)
    => Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);

    // Tidies a campaign before it is saved and brings an older one up to date after it is loaded.
    public void Normalize()
    {
        Name = (Name ?? string.Empty).Trim();
        Description = (Description ?? string.Empty).Trim();
        NpcFields = NpcFieldDefaults.Normalize(NpcFields);

        Factions ??= new List<Faction>();
        foreach (var faction in Factions)
        {
            faction.Name = (faction.Name ?? string.Empty).Trim();
            faction.Description = (faction.Description ?? string.Empty).Trim();
        }

        Day = Math.Max(1, Day);
        Hour = Math.Clamp(Hour, 0, Math.Max(1, Calendar.HoursPerDay) - 1);
        Characters ??= new List<SessionCharacterRecord>();
        AssetPackName = (AssetPackName ?? string.Empty).Trim();
        if (!Enum.IsDefined(typeof(CustomAssetPolicy), AssetPolicy)) AssetPolicy = CustomAssetPolicy.PromptMerge;

        SchemaVersion = 3;
    }

    // Returns what is wrong with the campaign, or null when it can be saved.
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) return "Give the campaign a name.";

        string? error = Calendar.Validate() ?? NpcFieldDefaults.Validate(NpcFields);
        if (error != null) return error;

        if (Factions.Any(f => string.IsNullOrWhiteSpace(f.Name))) return "Every faction needs a name.";
        if (Factions.Select(f => f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Factions.Count)
            return "Two factions share a name.";

        return null;
    }
}