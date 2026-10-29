using CharManJur.Models;

namespace CharManJur.Services;

// The GM's event log for a campaign: the newest events, kept live, with older ones archived on disk.
public interface IGmEventLog
{
    event Action? Changed;

    // Newest first. A new list is swapped in on every change, so this can be read from any thread.
    IReadOnlyList<PlayerActionLogEntry> Entries { get; }

    int ArchivedCount { get; }
    int ActiveLimit { get; }

    Task AttachAsync(Guid campaignId);
    Task DetachAsync();
    Task ImportAsync(IEnumerable<PlayerActionLogEntry> entries);
}