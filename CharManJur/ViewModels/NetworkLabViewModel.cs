using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// One player in the Network Lab roster: what the host sees and what the player's own side sees.
public class LabPlayerRow
{
    public LabPlayerRow(Guid playerId, string title, string hostSees, string playerSees)
    {
        PlayerId = playerId;
        Title = title;
        Summary = $"GM sees: {hostSees}   ·   Player sees: {playerSees}";
    }

    public Guid PlayerId { get; }
    public string Title { get; }
    public string Summary { get; }
}

// Developer screen for trying the network on ONE device: start a lab session, add players that live inside
// the app, kick them, cut their links and watch them reconnect, and slow or damage the network on purpose.
public class NetworkLabViewModel : INotifyPropertyChanged
{
    private const int TraceLimit = 300;

    private readonly IGmSessionHub _net;
    private int _labCounter;

    public NetworkLabViewModel(IGmSessionHub net)
    {
        _net = net;

        StartLabSessionCommand = new Command(async () => await RunAsync(StartLabSessionAsync, "Network Lab: start session"));
        StopLabSessionCommand = new Command(async () => await RunAsync(StopLabSessionAsync, "Network Lab: stop session"));
        AddLabPlayerCommand = new Command(async () => await RunAsync(AddLabPlayerAsync, "Network Lab: add player"));
        DropLinksCommand = new Command(async () => await RunAsync(() => _net.DropAllLinksAsync(), "Network Lab: drop links"));
        ResetChaosCommand = new Command(ResetChaos);
        CopyTraceCommand = new Command(async () => await Clipboard.Default.SetTextAsync(string.Join("\n", Trace)));
        ClearTraceCommand = new Command(() => Trace.Clear());
        CloseCommand = new Command(async () => await Application.Current!.MainPage!.Navigation.PopModalAsync());
    }

    public ObservableCollection<LabPlayerRow> Players { get; } = new();
    public ObservableCollection<string> Trace { get; } = new();

    public ICommand StartLabSessionCommand { get; }
    public ICommand StopLabSessionCommand { get; }
    public ICommand AddLabPlayerCommand { get; }
    public ICommand DropLinksCommand { get; }
    public ICommand ResetChaosCommand { get; }
    public ICommand CopyTraceCommand { get; }
    public ICommand ClearTraceCommand { get; }
    public ICommand CloseCommand { get; }

    // The page calls these when it appears and disappears, so a closed Lab never keeps listening.
    public void Start()
    {
        _net.Changed += OnNetChanged;
        _net.TraceAdded += OnTraceAdded;

        Trace.Clear();
        foreach (var line in _net.TraceLines) Trace.Add(line);
        Refresh();
    }

    public void Stop()
    {
        _net.Changed -= OnNetChanged;
        _net.TraceAdded -= OnTraceAdded;
    }

    // ===== STATUS =====
    public bool IsRunning => _net.IsRunning;
    public bool NoSession => !_net.IsRunning;
    public bool IsLabSession => _net.IsLabSession;
    public bool HasNoPlayers => Players.Count == 0;

    public string StatusText
    {
        get
        {
            if (!_net.IsRunning)
                return "No session is running. Start a lab session here, or open a campaign's session from the GM dashboard.";

            string kind = _net.IsLabSession ? "Lab session" : $"Campaign session \"{_net.SessionName}\"";
            string door = _net.IsOpen ? "open" : "closed";
            return $"{kind} is running ({door}). {_net.HostPlayers.Count} of 8 players.";
        }
    }

    // ===== NETWORK TROUBLE (applies to every link, takes effect immediately) =====
    public double LatencyMs
    {
        get => _net.Chaos.LatencyMs;
        set { _net.Chaos.LatencyMs = (int)Math.Round(value); RaiseChaos(); }
    }

    public double JitterMs
    {
        get => _net.Chaos.JitterMs;
        set { _net.Chaos.JitterMs = (int)Math.Round(value); RaiseChaos(); }
    }

    public double DropPercent
    {
        get => _net.Chaos.DropRate * 100;
        set { _net.Chaos.DropRate = Math.Clamp(value, 0, 100) / 100.0; RaiseChaos(); }
    }

    public double DuplicatePercent
    {
        get => _net.Chaos.DuplicateRate * 100;
        set { _net.Chaos.DuplicateRate = Math.Clamp(value, 0, 100) / 100.0; RaiseChaos(); }
    }

    public string LatencyText => $"Delay per message: {LatencyMs:0} ms";
    public string JitterText => $"Random extra delay: up to {JitterMs:0} ms";
    public string DropText => $"Lost messages: {DropPercent:0}%";
    public string DuplicateText => $"Doubled messages: {DuplicatePercent:0}%";

    private void ResetChaos()
    {
        _net.Chaos.LatencyMs = 0;
        _net.Chaos.JitterMs = 0;
        _net.Chaos.DropRate = 0;
        _net.Chaos.DuplicateRate = 0;
        RaiseChaos();
        OnPropertyChanged(nameof(LatencyMs));
        OnPropertyChanged(nameof(JitterMs));
        OnPropertyChanged(nameof(DropPercent));
        OnPropertyChanged(nameof(DuplicatePercent));
    }

    private void RaiseChaos()
    {
        OnPropertyChanged(nameof(LatencyText));
        OnPropertyChanged(nameof(JitterText));
        OnPropertyChanged(nameof(DropText));
        OnPropertyChanged(nameof(DuplicateText));
    }

    // ===== ACTIONS =====
    private async Task StartLabSessionAsync()
    {
        await _net.StartAsync(Guid.Empty, "Lab session");
        _net.SetOpen(true);
    }

    private Task StopLabSessionAsync() => _net.IsLabSession ? _net.StopAsync() : Task.CompletedTask;

    private async Task AddLabPlayerAsync()
    {
        if (!_net.IsRunning) return;

        _labCounter++;
        var hello = new HelloMessage
        {
            AppVersion = AppInfo.VersionString,
            PlayerId = Guid.NewGuid(),
            PlayerName = $"Lab Player {_labCounter}",
            CharacterGuid = Guid.NewGuid(),
            CharacterName = $"Lab Character {_labCounter}"
        };

        var result = await _net.JoinLocalPlayerAsync(hello);
        if (!result.Accepted)
            await Application.Current!.MainPage!.DisplayAlertAsync("Add Lab Player", $"Could not join: {result.Message}", "OK");
    }

    public async Task KickAsync(LabPlayerRow row) => await RunAsync(() => _net.KickAsync(row.PlayerId), "Network Lab: kick");

    public async Task LeaveAsync(LabPlayerRow row) => await RunAsync(() => _net.LeaveLocalPlayerAsync(row.PlayerId), "Network Lab: leave");

    // Lab actions never crash the app: a failure is logged and shown.
    private static async Task RunAsync(Func<Task> work, string where)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            CrashLog.Write(where, ex);
            await Application.Current!.MainPage!.DisplayAlertAsync("Network Lab", $"{where} failed: {ex.Message}", "OK");
        }
    }

    // ===== LIVE UPDATES (the hub raises these on pool threads) =====
    private void OnNetChanged() => MainThread.BeginInvokeOnMainThread(Refresh);

    private void OnTraceAdded(string line)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            Trace.Add(line);
            while (Trace.Count > TraceLimit) Trace.RemoveAt(0);
        });

    private void Refresh()
    {
        var locals = _net.LocalPlayers.ToDictionary(l => l.PlayerId);

        Players.Clear();
        foreach (var player in _net.HostPlayers)
        {
            locals.TryGetValue(player.PlayerId, out var local);
            Players.Add(new LabPlayerRow(
                player.PlayerId,
                $"{player.CharacterName} ({player.PlayerName})",
                player.IsConnected ? "connected" : "connection lost",
                local == null ? "n/a" : local.State.ToString()));
        }

        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(NoSession));
        OnPropertyChanged(nameof(IsLabSession));
        OnPropertyChanged(nameof(HasNoPlayers));
        OnPropertyChanged(nameof(StatusText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}