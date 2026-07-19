using Microsoft.Maui.Storage;

namespace CharManJur.Services;

/// <summary>
/// Placeholder for a real multiplayer campaign-membership identity.
/// Until a real campaign/join system exists, this returns one GUID
/// per app install, reused by every character created on this device.
/// When real campaign-join is built, only this class's generation
/// logic needs to change — everything reading CharacterData.PlayerId
/// stays the same.
/// </summary>
public static class InstallIdentity
{
    private const string PrefKey = "CharManJur_InstallPlayerId";

    public static Guid GetOrCreateInstallPlayerId()
    {
        string stored = Preferences.Default.Get(PrefKey, string.Empty);
        if (Guid.TryParse(stored, out Guid existing))
        {
            return existing;
        }

        Guid newId = Guid.NewGuid();
        Preferences.Default.Set(PrefKey, newId.ToString());
        return newId;
    }
}