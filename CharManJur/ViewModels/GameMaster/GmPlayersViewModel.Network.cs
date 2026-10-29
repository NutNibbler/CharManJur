using System.Collections.ObjectModel;
using System.Text.Json;
using CharManJur.Views;
using CharManJur.Views.Godrick_LiveGame;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// The network half of the Players panel: letting players on the local network join, the invite code and
// addresses to give them, and the players who joined that way.
public partial class GmPlayersViewModel
{
    private const string NetworkEnabledKey = "GmNetworkEnabled";

    private NetworkHostingInfo? _hosting;
    private string _networkProblem = string.Empty;
    private bool _networkEnabled = ReadNetworkEnabled();
    private ICommand? _copyInviteCommand;

    public ObservableCollection<RemotePlayerRow> RemotePlayers { get; } = new();
    public bool HasRemotePlayers => RemotePlayers.Count > 0;

    public ICommand CopyInviteCommand => _copyInviteCommand ??= new Command(async () => await CopyInviteAsync());

    // The GM's choice, remembered between runs. Off = only the test players inside this app can join.
    public bool NetworkEnabled
    {
        get => _networkEnabled;
        set
        {
            if (_networkEnabled == value) return;
            _networkEnabled = value;
            try { Preferences.Default.Set(NetworkEnabledKey, value); }
            catch (Exception ex) { CrashLog.Write("GM: saving the network setting", ex); }

            OnPropertyChanged();
            OnPropertyChanged(nameof(NetworkHint));
            _ = ApplyNetworkAsync();
        }
    }

    public bool HasNetwork => _hosting != null;
    public string InviteCode => _hosting?.InviteCode ?? string.Empty;
    public string AddressesText => _hosting == null ? string.Empty : string.Join("\n", _hosting.Addresses);

    public string NetworkSummary => _hosting == null
        ? string.Empty
        : $"Port {_hosting.Port} · " +
          (_hosting.PasswordRequired ? "password required" : "no password") + " · " +
          (_hosting.Announcing ? "players looking for sessions can see it" : "not announced: players need the code");

    public string NetworkHint => !_networkEnabled
        ? "Network joining is off. Only test players inside this app can join."
        : IsSessionOpen
            ? "Starting…"
            : "Open the session to get an invite code.";

    public string NetworkProblem
    {
        get => _networkProblem;
        private set
        {
            _networkProblem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNetworkProblem));
        }
    }

    public bool HasNetworkProblem => !string.IsNullOrEmpty(_networkProblem);

    public string FirewallHint =>
        "The invite code works for players on the same Wi-Fi or network. The first time, Windows may ask whether to " +
        "allow CharManJur through the firewall: choose Private networks.";

    // Called when the session opens, and when the GM flips the switch while it is open.
    public async Task ApplyNetworkAsync()
    {
        try
        {
            if (_net.IsRunning)
            {
                if (_networkEnabled) await _net.StartNetworkAsync();
                else await _net.StopNetworkAsync();
                NetworkProblem = string.Empty;
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: opening the session to the network", ex);
            NetworkProblem = $"Players on the network can't join: {ex.Message}";
        }

        RefreshNetwork();
    }

    // Re-reads what the hub says about the network (also after the session closes).
    public void RefreshNetwork() => OnMain(() =>
    {
        _hosting = _net.Network;
        OnPropertyChanged(nameof(HasNetwork));
        OnPropertyChanged(nameof(InviteCode));
        OnPropertyChanged(nameof(AddressesText));
        OnPropertyChanged(nameof(NetworkSummary));
        OnPropertyChanged(nameof(NetworkHint));
    });

    // The players who joined over the network, as the hub reports them (the dashboard works out which those are).
    public void SetRemotePlayers(IReadOnlyList<PlayerConnection> players) => OnMain(() =>
    {
        foreach (var gone in RemotePlayers.Where(r => players.All(p => p.PlayerId != r.PlayerId)).ToList())
            RemotePlayers.Remove(gone);

        foreach (var player in players)
        {
            var row = RemotePlayers.FirstOrDefault(r => r.PlayerId == player.PlayerId);
            if (row == null)
            {
                row = new RemotePlayerRow(player.PlayerId, $"{player.CharacterName} ({player.PlayerName})", player.IsConnected);
                RemotePlayers.Add(row);
            }
            else
            {
                row.IsConnected = player.IsConnected;
            }

            row.Apply(_net.SummaryFor(player.PlayerId));
        }

        OnPropertyChanged(nameof(HasRemotePlayers));
        OnPropertyChanged(nameof(HasNoPlayers));
    });

    public async Task KickRemoteAsync(RemotePlayerRow row)
    {
        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Kick Player",
            $"Remove {row.Title} from this session? They stay out until you add them again.",
            "Kick", "Cancel");
        if (!ok) return;

        await _net.KickAsync(row.PlayerId);   // the dashboard takes the card off when the session reports they left
        if (_log != null) await _log(PlayerActionType.PlayerKicked, $"Kicked {row.Title} from the session");
    }

    // ===== SENDING DAMAGE AND EFFECTS TO A NETWORK PLAYER =====
    // The same screens as for test players; the result travels to the player's app, which queues it on their
    // live character. The GM is told whether the player's app confirmed it.
    public async Task ApplyDamageRemoteAsync(RemotePlayerRow row)
    {
        await PushAsync(new GmSendDamagePage(
            row.Title,
            damage => _ = SendRemoteAsync(row, "damage", damage.Summary, JsonSerializer.Serialize(damage), PlayerActionType.IncomingDamageSent)));
    }

    public async Task ApplyEffectRemoteAsync(RemotePlayerRow row)
    {
        await PushAsync(new ApplyEffectPopup(
            effect => _ = SendRemoteAsync(row, "effect", $"The effect {effect.Name}",
                JsonSerializer.Serialize(new IncomingEffect { Effect = effect }), PlayerActionType.IncomingEffectSent)));
    }

    private async Task SendRemoteAsync(RemotePlayerRow row, string kind, string what, string json, PlayerActionType logType)
    {
        try
        {
            // One id for every try, so the player's app can tell a resend from a new action.
            var action = new GmActionMessage { ActionId = Guid.NewGuid(), Kind = kind, Json = json };

            while (true)
            {
                var result = await _net.SendActionAsync(row.PlayerId, action, TimeSpan.FromSeconds(8));
                if (result.Delivered)
                {
                    GameNotices.Post(new GameNotice("📨 Sent",
                        $"{what} sent to {row.Title}. Their button flashes until they respond.", NoticeSource.Player, 6));
                    if (_log != null) await _log(logType, $"Sent {what} to {row.Title}");
                    return;
                }

                bool again = await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Not Confirmed", $"{what} to {row.Title}: {result.Message}", "Send Again", "Cancel");
                if (!again) return;
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: sending an action to a network player", ex);
        }
    }

    private async Task CopyInviteAsync()
    {
        string code = _hosting?.InviteCode ?? string.Empty;
        if (code.Length == 0) return;

        try
        {
            await Clipboard.Default.SetTextAsync(code);
            GameNotices.Post(new GameNotice("📋 Copied", "The invite code is on your clipboard.", NoticeSource.Gm, 4));
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: copying the invite code", ex);
        }
    }

    private static bool ReadNetworkEnabled()
    {
        try { return Preferences.Default.Get(NetworkEnabledKey, true); }
        catch { return true; }
    }

    private static void OnMain(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }
}