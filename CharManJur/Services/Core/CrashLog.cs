namespace CharManJur.Services;

// Writes unhandled errors to %LocalAppData%\CharManJur\crash.log so a published build can say what happened.
public static class CrashLog
{
    private static readonly object Gate = new();
    public static string FilePath => Path.Combine(AppPaths.DataDirectory, "crash.log");

    public static void Hook()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Write("AppDomain unhandled" + (e.IsTerminating ? " (terminating)" : ""), e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Write("Unobserved task exception", e.Exception);
            e.SetObserved();
        };
    }

    public static void Write(string where, Exception? ex)
    {
        try
        {
            lock (Gate)
                File.AppendAllText(FilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {where}\n{ex}\n\n");
        }
        catch { /* logging must never throw */ }
    }

    // Runs a command body; on failure logs it and shows the error instead of crashing the app.
    public static async Task GuardAsync(Func<Task> action, string where)
    {
        try { await action(); }
        catch (Exception ex)
        {
            Write(where, ex);
            try
            {
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Something went wrong",
                    $"{ex.GetType().Name}: {ex.Message}\n\nDetails were saved to:\n{FilePath}",
                    "OK");
            }
            catch { }
        }
    }
}