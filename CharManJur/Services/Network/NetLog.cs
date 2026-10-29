namespace CharManJur.Services;

// Network trace sink. Core network code logs here and never touches files or UI itself.
// The app (or the Network Lab) sets Sink to write to the log file / a live trace list.
// Never log passwords, keys or full character data.
public static class NetLog
{
    public static Action<string>? Sink { get; set; }

    public static void Info(string message) => Write("INFO ", message);
    public static void Warn(string message) => Write("WARN ", message);
    public static void Error(string message, Exception? ex = null)
        => Write("ERROR", ex == null ? message : $"{message}: {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        try { Sink?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {level} {message}"); }
        catch { /* logging must never throw */ }
    }
}