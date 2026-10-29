using CharManJur.Models;

namespace CharManJur.Services;

// While a scope is open, everything the action log records on this flow of work carries the scope's source
// and session. The GM's bestow panel uses it so changes it makes to a character are logged as the GM's.
public static class ActionLogScope
{
    private sealed record Scope(ActionSource Source, Guid? SessionId);

    private static readonly AsyncLocal<Scope?> Current = new();

    public static ActionSource? Source => Current.Value?.Source;
    public static Guid? SessionId => Current.Value?.SessionId;

    public static IDisposable Begin(ActionSource source, Guid? sessionId)
    {
        var previous = Current.Value;
        Current.Value = new Scope(source, sessionId);
        return new Restore(previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly Scope? _previous;
        private bool _done;

        public Restore(Scope? previous) => _previous = previous;

        public void Dispose()
        {
            if (_done) return;
            _done = true;
            Current.Value = _previous;
        }
    }
}