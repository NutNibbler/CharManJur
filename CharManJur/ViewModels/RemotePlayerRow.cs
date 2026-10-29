using System.ComponentModel;
using System.Runtime.CompilerServices;
using CharManJur.Models;

namespace CharManJur.ViewModels;

// One stat chip on a network player's card. The names match the chips on the test players' cards.
public class RemoteChip
{
    public RemoteChip(string label, string value, int level)
    {
        Label = label;
        Value = value;
        IsReduced = level == 1;
        IsCritical = level >= 2;
    }

    public string Label { get; }
    public string Value { get; }
    public bool IsReduced { get; }
    public bool IsCritical { get; }
}

// One player who joined over the network, as the Players panel lists them: connection state plus the live
// stat chips their app keeps sending. GM actions for it arrive with the "GM actions over the wire" step.
public class RemotePlayerRow : INotifyPropertyChanged
{
    private bool _isConnected;
    private IReadOnlyList<RemoteChip> _chips = Array.Empty<RemoteChip>();
    private int _waiting;

    public RemotePlayerRow(Guid playerId, string title, bool isConnected)
    {
        PlayerId = playerId;
        Title = title;
        _isConnected = isConnected;
    }

    public Guid PlayerId { get; }
    public string Title { get; }
    public bool IsDisconnected => !_isConnected;
    public string StatusText => _isConnected ? "🟢 Connected" : "🔴 Connection lost";

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected == value) return;
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDisconnected));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public IReadOnlyList<RemoteChip> Chips => _chips;
    public bool HasChips => _chips.Count > 0;
    public bool HasNoChips => _chips.Count == 0;
    public bool HasWaiting => _waiting > 0;
    public string WaitingText => $"📨 {_waiting} waiting";

    // Takes the latest chips the player sent (null = nothing received yet). Call on the UI thread.
    public void Apply(CharacterSummaryMessage? summary)
    {
        var chips = summary == null
            ? Array.Empty<RemoteChip>()
            : summary.Chips.Select(c => new RemoteChip(c.Label, c.Value, c.Level)).ToArray();
        int waiting = summary?.Waiting ?? 0;

        bool same = waiting == _waiting
            && chips.Length == _chips.Count
            && chips.Zip(_chips, (a, b) => a.Label == b.Label && a.Value == b.Value && a.IsReduced == b.IsReduced && a.IsCritical == b.IsCritical).All(x => x);
        if (same) return;

        _chips = chips;
        _waiting = waiting;
        OnPropertyChanged(nameof(Chips));
        OnPropertyChanged(nameof(HasChips));
        OnPropertyChanged(nameof(HasNoChips));
        OnPropertyChanged(nameof(HasWaiting));
        OnPropertyChanged(nameof(WaitingText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}