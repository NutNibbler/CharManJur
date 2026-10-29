using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

// Listens to the shared action log while a campaign is open on the GM dashboard. Keeps the newest 300 events in
// the campaign's folder and moves older ones to an archive file (one JSON entry per line) that is only kept on disk.
public sealed class GmEventLog : IGmEventLog
{
    public const int Limit = 300;

    private const string ActiveFileName = "events.json";
    private const string ArchiveFileName = "events.archive.jsonl";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly IPlayerActionLogService _source;
    private readonly ICampaignStore _store;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _io = new(1, 1);
    private readonly List<PlayerActionLogEntry> _pendingArchive = new();

    private volatile IReadOnlyList<PlayerActionLogEntry> _entries = Array.Empty<PlayerActionLogEntry>();
    private string? _folder;
    private int _archived;
    private bool _subscribed;

    public GmEventLog(IPlayerActionLogService source, ICampaignStore store)
    {
        _source = source;
        _store = store;
    }

    public event Action? Changed;

    public IReadOnlyList<PlayerActionLogEntry> Entries => _entries;
    public int ArchivedCount => Volatile.Read(ref _archived);
    public int ActiveLimit => Limit;

    // ===== ATTACH / DETACH =====
    public async Task AttachAsync(Guid campaignId)
    {
        await DetachAsync();

        string folder = _store.CampaignFolder(campaignId);
        Directory.CreateDirectory(folder);

        var loaded = await Task.Run(() => ReadActive(folder));
        int archived = await Task.Run(() => CountArchive(folder));

        loaded = loaded.OrderByDescending(e => e.Timestamp).ToList();
        var overflow = new List<PlayerActionLogEntry>();
        if (loaded.Count > Limit)
        {
            overflow = loaded.Skip(Limit).ToList();
            loaded = loaded.Take(Limit).ToList();
        }

        lock (_gate)
        {
            _folder = folder;
            _entries = loaded;
            _archived = archived + overflow.Count;
            _pendingArchive.Clear();
            _pendingArchive.AddRange(overflow);

            if (!_subscribed)
            {
                _source.EntryLogged += OnEntryLogged;
                _subscribed = true;
            }
        }

        if (overflow.Count > 0) await PersistAsync();
        Changed?.Invoke();
    }

    public async Task DetachAsync()
    {
        bool wasAttached;
        lock (_gate)
        {
            wasAttached = _folder != null;
            if (_subscribed)
            {
                _source.EntryLogged -= OnEntryLogged;   // no new entries from here on
                _subscribed = false;
            }
        }
        if (!wasAttached) return;

        await PersistAsync();   // write whatever is still waiting

        lock (_gate)
        {
            _folder = null;
            _entries = Array.Empty<PlayerActionLogEntry>();
            _archived = 0;
            _pendingArchive.Clear();
        }
        Changed?.Invoke();
    }

    // ===== ADDING =====
    // May arrive on a background thread.
    private void OnEntryLogged(PlayerActionLogEntry entry) => Add(new[] { entry });

    public Task ImportAsync(IEnumerable<PlayerActionLogEntry> entries)
    {
        Add(entries);
        return Task.CompletedTask;
    }

    private void Add(IEnumerable<PlayerActionLogEntry> incoming)
    {
        bool changed = false;

        lock (_gate)
        {
            if (_folder == null) return;

            var list = new List<PlayerActionLogEntry>(_entries);
            var known = new HashSet<Guid>(list.Select(e => e.Id));
            foreach (var entry in incoming)
            {
                if (known.Add(entry.Id))
                {
                    list.Add(entry);
                    changed = true;
                }
            }
            if (!changed) return;

            list = list.OrderByDescending(e => e.Timestamp).ToList();
            if (list.Count > Limit)
            {
                var overflow = list.Skip(Limit).ToList();
                list = list.Take(Limit).ToList();
                _pendingArchive.AddRange(overflow);
                _archived += overflow.Count;
            }
            _entries = list;
        }

        _ = PersistAsync();
        Changed?.Invoke();
    }

    // ===== DISK =====
    // Always writes the latest state, so overlapping saves can never leave an older copy behind.
    private async Task PersistAsync()
    {
        await _io.WaitAsync();
        try
        {
            string? folder;
            IReadOnlyList<PlayerActionLogEntry> active;
            List<PlayerActionLogEntry> archive;

            lock (_gate)
            {
                folder = _folder;
                active = _entries;
                archive = _pendingArchive.ToList();
                _pendingArchive.Clear();
            }
            if (folder == null) return;

            if (archive.Count > 0)
            {
                var lines = archive.OrderBy(e => e.Timestamp).Select(e => JsonSerializer.Serialize(e, Options));
                await File.AppendAllLinesAsync(Path.Combine(folder, ArchiveFileName), lines);
            }

            string path = Path.Combine(folder, ActiveFileName);
            string temp = path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(active, Options));
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GM event log save error: {ex.Message}");
        }
        finally
        {
            _io.Release();
        }
    }

    private static List<PlayerActionLogEntry> ReadActive(string folder)
    {
        try
        {
            string path = Path.Combine(folder, ActiveFileName);
            if (!File.Exists(path)) return new List<PlayerActionLogEntry>();
            return JsonSerializer.Deserialize<List<PlayerActionLogEntry>>(File.ReadAllText(path), Options)
                   ?? new List<PlayerActionLogEntry>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GM event log read error: {ex.Message}");
            return new List<PlayerActionLogEntry>();
        }
    }

    private static int CountArchive(string folder)
    {
        try
        {
            string path = Path.Combine(folder, ArchiveFileName);
            return File.Exists(path) ? File.ReadLines(path).Count(l => !string.IsNullOrWhiteSpace(l)) : 0;
        }
        catch
        {
            return 0;
        }
    }
}