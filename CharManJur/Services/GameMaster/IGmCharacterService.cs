using CharManJur.Models;

namespace CharManJur.Services;

// A saved character opened for the GM to change. Svc is a character service of its own, filled from the
// save file and tied to no screen; Log marks everything it records as the GM's.
public sealed class GmCharacterEdit
{
    public CharacterSaveData Recipient { get; init; } = null!;
    public ICharAttribDataService Svc { get; init; } = null!;
    public IPlayerActionLogService Log { get; init; } = null!;
}

// What the GM's Player Viewer is built on: a character that is never saved, and a log that records nothing.
public sealed class GmViewerSource
{
    public ICharAttribDataService Svc { get; init; } = null!;
    public IPlayerActionLogService Log { get; init; } = null!;
    public ICharacterPersistenceService Persistence { get; init; } = null!;
}

public interface IGmCharacterService
{
    // Null when the character's save file can't be found.
    Task<GmCharacterEdit?> OpenAsync(CharacterSaveData recipient, Guid? sessionId);

    LiveSubFeatures NewSubFeatures(GmCharacterEdit edit);
    // A read-only copy of a saved character for the Player Viewer. Null when the save file can't be found.
    Task<GmViewerSource?> OpenViewerAsync(CharacterSaveData recipient);

    // Writes the character's final state to its file and reads it back to confirm. False if it couldn't be confirmed.
    Task<bool> SaveAsync(GmCharacterEdit edit);
    // A player's stats for their card: every stat as a chip, plus the one-line version, read the way the live game shows them.
    Task<GmPlayerSummary> GetSummaryAsync(CharacterSaveData recipient);
}