using System.ComponentModel;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// One familiar in the dashboard's equipment column: name, Hit Protection with a ticker, and pack use.
public class FamiliarStripItem : INotifyPropertyChanged
{
    private readonly ICharAttribDataService _svc;

    public Familiar Familiar { get; }
    public Guid InstanceId => Familiar.InstanceId;

    public string Name => Familiar.HasGivenName ? Familiar.FmlrPlayerName! : (Familiar.FmlrName ?? "(unnamed)");
    // 🐾 for animals, ⚙️ for constructs (inanimate familiars).
    public string Icon => Familiar.FmlrIntelligence == FmlrIntelligences.Inanimate ? "⚙️" : "🐾";
    public string DisplayLabel => $"{Icon} {Name}";
    public bool IsDead => Familiar.IsDead;

    // Constructs (Inanimate) have no Hit Protection to show.
    public bool ShowHp => Familiar.FmlrIntelligence != FmlrIntelligences.Inanimate && Familiar.HP > 0 && !Familiar.IsDead;
    public string HpText => $"{Familiar.CurrentHP} / {Familiar.HP}";

    public string PackText => $"{Familiar.InventorySlotsUsed} / {Familiar.InventorySlotsAvailable}";
    public bool IsEncumbered => Familiar.IsEncumbered;

    public ICommand HpUpCommand { get; }
    public ICommand HpDownCommand { get; }

    public FamiliarStripItem(ICharAttribDataService svc, Familiar familiar)
    {
        _svc = svc;
        Familiar = familiar;
        HpUpCommand = new Command(() => ChangeHp(+1));
        HpDownCommand = new Command(() => ChangeHp(-1));
    }

    private void ChangeHp(int delta)
    {
        if (Familiar.IsDead) return;
        _svc.AdjustFamiliarHitpoints(InstanceId, delta);   // clamps and autosaves
        Refresh();
    }

    public void Refresh() => OnPropertyChanged(string.Empty);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}