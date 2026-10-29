namespace CharManJur.Services;

// Whether this app is playing in a session run by a GM host (multiplayer, not built yet).
// Every "only the GM may" rule asks here. Networking sets it when a player joins or leaves a session.
public static class SessionMode
{
    private static bool _gmHosted;

    public static bool GmHosted => _gmHosted;

    public static event Action? Changed;

    public static void SetGmHosted(bool hosted)
    {
        if (_gmHosted == hosted) return;
        _gmHosted = hosted;
        Changed?.Invoke();
    }
}