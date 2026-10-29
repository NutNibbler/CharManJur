using CharManJur.Models;

namespace CharManJur.Services;

// The player's side of a GM-hosted session: one connection at a time, held for as long as the app runs.
public interface IPlayerSession
{
    bool IsInSession { get; }
    ClientState State { get; }
    string SessionName { get; }
    string CharacterName { get; }

    // Raised on the UI thread whenever any of the above changes.
    event Action? Changed;

    // Connects, loads the character into the app (like Load Selected) and switches the app to GM-hosted play.
    Task<JoinResult> JoinAsync(string address, CharacterSaveData character, string? password);

    // Sends the GM the character as it is saved on this device right now and waits for the GM to confirm it is stored.
    // True when confirmed (or when there is no session). Call it after saving, before leaving.
    Task<bool> SendFinalCopyAsync();

    // Leaves on purpose.
    Task LeaveAsync();
}