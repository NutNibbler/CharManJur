using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.ViewModels;

// The Players panel: opening and closing the session, the asset policy, the session password, invitations
// (not built yet) and the connected players, each with Kick, Send Damage, Send Effect and the bestow pages.
public partial class GmPlayersViewModel : INotifyPropertyChanged
{
    private readonly ICampaignStore _store;
    private readonly IGmCharacterService _characters;
    private readonly IItemDataService _items;
    private readonly IFamiliarDataService _familiars;
    private readonly IGmBestowService _bestow;
    private readonly IGmSessionHub _net;

    private ObservableCollection<GmPlayerRow>? _players;
    private Func<Campaign?>? _campaign;
    private Func<Guid?>? _runId;
    private Func<PlayerActionType, string, Task>? _log;
    private Func<Task>? _playersChanged;
    private Action<GmPlayerRow>? _removePlayer;
    private Func<bool>? _isOpen;
    private Func<Task>? _toggleSession;

    private string _newPassword = string.Empty;
    private string _passwordError = string.Empty;
    private bool _busy;

    public GmPlayersViewModel(
    ICampaignStore store,
    IGmCharacterService characters,
    IItemDataService items,
    IFamiliarDataService familiars,
    IGmBestowService bestow,
    IGmSessionHub net)
    {
        _store = store;
        _characters = characters;
        _items = items;
        _familiars = familiars;
        _bestow = bestow;
        _net = net;

        SetPasswordCommand = new Command(async () => await SetPasswordAsync());
        RemovePasswordCommand = new Command(async () => await RemovePasswordAsync());
        ToggleSessionCommand = new Command(async () => { if (_toggleSession != null) await _toggleSession(); });
        OpenNetworkLabCommand = new Command(async () => await PushAsync(new NetworkLabPage(_net)));
    }

    public ICommand SetPasswordCommand { get; }
    public ICommand RemovePasswordCommand { get; }
    public ICommand ToggleSessionCommand { get; }
    public ICommand OpenNetworkLabCommand { get; }

    public ObservableCollection<GmPlayerRow>? Players => _players;
    public bool HasNoPlayers => (_players == null || _players.Count == 0) && RemotePlayers.Count == 0;

    // Wired up by the dashboard.
    public void Attach(
        ObservableCollection<GmPlayerRow> players,
        Func<Campaign?> campaign,
        Func<Guid?> runId,
        Func<PlayerActionType, string, Task> log,
        Func<Task> playersChanged,
        Action<GmPlayerRow> removePlayer,
        Func<bool> isOpen,
        Func<Task> toggleSession)
    {
        _players = players;
        _campaign = campaign;
        _runId = runId;
        _log = log;
        _playersChanged = playersChanged;
        _removePlayer = removePlayer;
        _isOpen = isOpen;
        _toggleSession = toggleSession;

        players.CollectionChanged += (_, _) => MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(HasNoPlayers)));
        OnPropertyChanged(nameof(Players));
        OnPropertyChanged(nameof(HasNoPlayers));
    }

    // A campaign was loaded: show its session, policy and password state.
    public void RefreshSession()
    {
        _newPassword = string.Empty;
        PasswordError = string.Empty;
        OnPropertyChanged(nameof(NewPassword));
        OnPropertyChanged(nameof(CanSetPassword));
        RaisePasswordProperties();
        RaiseSessionState();
        OnPropertyChanged(nameof(PolicyIndex));
        OnPropertyChanged(nameof(PolicyDescription));
    }

    // The session was opened or closed.
    public void RaiseSessionState()
    {
        OnPropertyChanged(nameof(IsSessionOpen));
        OnPropertyChanged(nameof(SessionStateText));
        OnPropertyChanged(nameof(SessionButtonText));
        OnPropertyChanged(nameof(SessionHint));
        OnPropertyChanged(nameof(NetworkHint));
    }

    // ===== SESSION OPEN / CLOSED =====
    public bool IsSessionOpen => _isOpen?.Invoke() == true;

    public string SessionStateText => IsSessionOpen
        ? "🟢 OPEN: players can join"
        : "🔴 CLOSED: nobody can join";

    public string SessionButtonText => IsSessionOpen ? "🔒 Close Session" : "🔓 Open Session";

    public string SessionHint => IsSessionOpen
        ? "Closing saves every connected player, copies their characters into the campaign, then removes them from the session."
        : "Open the session to let players join. It starts closed every time the campaign is opened.";

    // ===== ASSET POLICY =====
    // What happens to the custom assets a player brings. Changeable at any time; it only affects players as they join.
    public List<string> PolicyLabels { get; } = AssetPolicyInfo.Labels.ToList();

    public int PolicyIndex
    {
        get => (int)(_campaign?.Invoke()?.AssetPolicy ?? CustomAssetPolicy.PromptMerge);
        set
        {
            var campaign = _campaign?.Invoke();
            if (campaign == null || value < 0 || value >= PolicyLabels.Count || (int)campaign.AssetPolicy == value) return;

            campaign.AssetPolicy = (CustomAssetPolicy)value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PolicyDescription));
            _ = _store.SaveStateAsync(campaign);
        }
    }

    public string PolicyDescription => AssetPolicyInfo.DescriptionFor(_campaign?.Invoke()?.AssetPolicy ?? CustomAssetPolicy.PromptMerge);

    // ===== SESSION PASSWORD =====
    public bool HasPassword => _campaign?.Invoke()?.HasPassword == true;
    public bool HasNoPassword => !HasPassword;

    public string PasswordStatus => HasPassword
        ? "🔒 A password is set. Players must enter it to join."
        : "No password: anyone with the invitation can join.";

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            _newPassword = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSetPassword));
        }
    }

    public bool CanSetPassword => _newPassword.Length > 0;

    public string PasswordError
    {
        get => _passwordError;
        private set
        {
            _passwordError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPasswordError));
        }
    }

    public bool HasPasswordError => !string.IsNullOrEmpty(_passwordError);

    private async Task SetPasswordAsync()
    {
        var campaign = _campaign?.Invoke();
        if (campaign == null) return;

        if (_newPassword.Length == 0)
        {
            PasswordError = "Type a password first.";
            return;
        }

        campaign.SetPassword(_newPassword);
        await _store.SaveStateAsync(campaign);

        NewPassword = string.Empty;
        PasswordError = string.Empty;
        RaisePasswordProperties();
    }

    private async Task RemovePasswordAsync()
    {
        var campaign = _campaign?.Invoke();
        if (campaign == null) return;

        campaign.SetPassword(null);
        await _store.SaveStateAsync(campaign);

        PasswordError = string.Empty;
        RaisePasswordProperties();
    }

    private void RaisePasswordProperties()
    {
        OnPropertyChanged(nameof(HasPassword));
        OnPropertyChanged(nameof(HasNoPassword));
        OnPropertyChanged(nameof(PasswordStatus));
    }

    // ===== KICK =====
    // Disconnects the player from the session. The dashboard takes their row off the list when the session
    // reports that they left. If they are no longer in the session, they are simply taken off the list.
    public async Task KickAsync(GmPlayerRow row)
    {
        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Kick Player",
            $"Remove {row.Title} from this session? They stay out until you add them again.",
            "Kick", "Cancel");
        if (!ok) return;

        bool kicked = await _net.KickAsync(row.PlayerId);
        if (!kicked) _removePlayer?.Invoke(row);
        if (_log != null) await _log(PlayerActionType.PlayerKicked, $"Kicked {row.Title} from the session");
    }

    // ===== SENDING DAMAGE AND EFFECTS =====
    // The GM never applies these. They are sent to the player, who takes them on from their own screen,
    // so any roll that follows (a Vigor save, a Shifter's Stability save) is the player's to make.
    public async Task ApplyDamageAsync(GmPlayerRow row)
    {
        await PushAsync(new GmSendDamagePage(
            row.Save.CharacterName,
            damage => _ = SendAsync(row, damage.Summary, svc => svc.QueueIncomingDamage(damage))));
    }

    public async Task ApplyEffectAsync(GmPlayerRow row)
    {
        await PushAsync(new ApplyEffectPopup(
            effect => _ = SendAsync(row, $"The effect {effect.Name}", svc => svc.QueueIncomingEffect(new IncomingEffect { Effect = effect }))));
    }

    private async Task SendAsync(GmPlayerRow row, string what, Action<ICharAttribDataService> queue)
    {
        var edit = await OpenAsync(row);
        if (edit == null) return;

        queue(edit.Svc);
        await FinishAsync(edit);

        GameNotices.Post(new GameNotice(
            "📨 Sent",
            $"{what} sent to {row.Save.CharacterName}. Their button flashes until they respond.",
            NoticeSource.Player, 6));
    }

    // ===== CHANGING A PLAYER'S CHARACTER =====
    public async Task BestowItemAsync(GmPlayerRow row)
    {
        var edit = await OpenAsync(row);
        if (edit == null) return;

        await PushAsync(new BestowItemPage(_items, edit.Svc, edit.Log, () => _ = FinishAsync(edit)));
    }

    public async Task BestowFamiliarAsync(GmPlayerRow row)
    {
        var edit = await OpenAsync(row);
        if (edit == null) return;

        await PushAsync(new BestowFamiliarPage(_familiars, edit.Svc, () => _ = FinishAsync(edit)));
    }

    public async Task BestowSubFeatureAsync(GmPlayerRow row)
    {
        const string spell = "📜 Spell";
        const string quip = "⚡ Quip";
        const string technique = "🌀 Technique";
        const string blueprint = "📐 Blueprint";

        string? choice = await Application.Current!.MainPage!.DisplayActionSheetAsync(
            $"Bestow to {row.Save.CharacterName}", "Cancel", null, spell, quip, technique, blueprint);
        if (choice == null || choice == "Cancel") return;

        var type = choice == spell ? SubFeatureType.Spell
                 : choice == quip ? SubFeatureType.Quip
                 : choice == technique ? SubFeatureType.Technique
                 : SubFeatureType.Blueprint;

        var edit = await OpenAsync(row);
        if (edit == null) return;

        await PushAsync(new BestowSubFeaturePage(_characters.NewSubFeatures(edit), type, () => _ = FinishAsync(edit)));
    }

    public async Task BestowScratchPageAsync(GmPlayerRow row)
    {
        var campaign = _campaign?.Invoke();
        if (campaign == null) return;
        var page = Application.Current!.MainPage!;

        var pages = await _bestow.GetPadPagesAsync(campaign.Id);
        if (pages.Count == 0)
        {
            await page.DisplayAlertAsync("Scratch Pad Pages", "Your pad has no pages yet. Make one in the Pad panel first.", "OK");
            return;
        }

        var labels = new Dictionary<string, ScratchPageInfo>();
        foreach (var p in pages)
        {
            string label = $"{(p.Type == ScratchPageType.Drawing ? "🖊️" : "📝")} {p.Name}";
            string unique = label;
            int n = 2;
            while (labels.ContainsKey(unique)) unique = $"{label} ({n++})";
            labels[unique] = p;
        }

        string? choice = await page.DisplayActionSheetAsync(
            $"Give a page to {row.Save.CharacterName}", "Cancel", null, labels.Keys.ToArray());
        if (choice == null || !labels.TryGetValue(choice, out var picked)) return;

        var line = new GmBestowLine { Kind = GmBestowKind.ScratchPage, Page = picked, CampaignId = campaign.Id };
        var outcomes = await _bestow.BestowAsync(new[] { row.Save }, new[] { line }, _runId?.Invoke());

        await page.DisplayAlertAsync("Scratch Pad Page", string.Join("\n", outcomes.Select(o => o.Summary)), "OK");
        if (_playersChanged != null) await _playersChanged();
    }

    // Loads the player's saved character for the GM to change.
    private async Task<GmCharacterEdit?> OpenAsync(GmPlayerRow row)
    {
        if (_busy) return null;
        _busy = true;
        try
        {
            var edit = await _characters.OpenAsync(row.Save, _runId?.Invoke());
            if (edit == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Player Not Found", $"{row.Title}'s save file wasn't found.", "OK");
            }
            return edit;
        }
        finally
        {
            _busy = false;
        }
    }

    // Runs when one of the pages reports a change: save the character and refresh the player strip.
    private async Task FinishAsync(GmCharacterEdit edit)
    {
        bool saved = await _characters.SaveAsync(edit);
        if (_playersChanged != null) await _playersChanged();

        if (!saved)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Save Not Confirmed",
                $"The changes to {edit.Recipient.CharacterName} couldn't be confirmed as saved. Check the character before trying again.",
                "OK");
        }
    }

    private static Task PushAsync(Page page) => Application.Current!.MainPage!.Navigation.PushModalAsync(page);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}