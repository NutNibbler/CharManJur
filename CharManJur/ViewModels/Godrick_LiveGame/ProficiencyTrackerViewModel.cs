using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class DamageTypeRow
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string KillsText { get; set; } = string.Empty;
    public ICommand UpCommand { get; set; } = null!;
    public ICommand DownCommand { get; set; } = null!;
}

// One skill of a weapon (Bleed VI, Sweep VII...). The title carries the one padlock; tap it for what it does.
public class WeaponSkillInfo
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICommand ExplainCommand { get; set; } = null!;
}

// One weapon whose skills are tracked: kills toward unlocking them, and/or a Warrior pick.
public class WeaponSkillRow
{
    public string Name { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string KillsText { get; set; } = string.Empty;
    public bool HasKills { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public bool HasNote => NoteText.Length > 0;
    public List<WeaponSkillInfo> Skills { get; set; } = new();
    public ICommand UpCommand { get; set; } = null!;
    public ICommand DownCommand { get; set; } = null!;
}

// The proficiency section of the Bio Sheet: three damage types, and the skills of each weapon you've earned or picked.
public class ProficiencyTrackerViewModel : INotifyPropertyChanged
{
    private readonly WeaponProficiencyTracker _tracker;
    private readonly IItemDataService _items;
    private const int Max = WeaponProficiencyTracker.KillsRequired;

    public ObservableCollection<DamageTypeRow> TypeRows { get; } = new();
    public ObservableCollection<WeaponSkillRow> WeaponRows { get; } = new();

    public bool HasNoWeapons => WeaponRows.Count == 0;

    // Shown until the player has picked their starting damage type.
    public bool NeedsStartingChoice => _tracker.NeedsStartingChoice;
    public ICommand ChooseStartingCommand { get; }

    public ProficiencyTrackerViewModel(WeaponProficiencyTracker tracker, IItemDataService items)
    {
        _tracker = tracker;
        _items = items;
        ChooseStartingCommand = new Command(async () => await ChooseStartingAsync());
    }

    private async Task ChooseStartingAsync()
    {
        if (await ProficiencyPrompts.AskStartingTypeAsync(_tracker, "Cancel")) await RefreshAsync();
    }

    private static string KeyOf(WeaponKillRecord r) => r.TemplateGuid.HasValue ? $"g:{r.TemplateGuid}" : $"i:{r.TemplateId}";

    public async Task RefreshAsync()
    {
        // ----- damage types -----
        TypeRows.Clear();
        foreach (var type in Enum.GetValues<WeaponDamageType>())
        {
            var progress = _tracker.TypeProgress(type);
            bool proficient = _tracker.IsTypeProficient(type);

            TypeRows.Add(new DamageTypeRow
            {
                Name = WeaponProficiencyTracker.Display(type),
                Description = EnumDisplayHelper.GetDescription(type) ?? string.Empty,
                KillsText = $"{progress.Kills} / {Max}",
                StatusText = !proficient ? "🔒 Not proficient yet"
                    : progress.Granted ? "✅ Proficient (starting choice)"
                    : "✅ Proficient",
                UpCommand = new Command(async () => { await _tracker.SetTypeKillsAsync(type, progress.Kills + 1); await RefreshAsync(); }),
                DownCommand = new Command(async () => { await _tracker.SetTypeKillsAsync(type, progress.Kills - 1); await RefreshAsync(); })
            });
        }

        // ----- weapon skills: weapons with kills and Warrior picks, merged -----
        var entries = new Dictionary<string, (WeaponKillRecord? Kills, WeaponKillRecord? Grant)>();
        foreach (var w in _tracker.State.Weapons)
            entries[KeyOf(w)] = (w, null);
        foreach (var g in _tracker.State.GrantedSkills)
            entries[KeyOf(g)] = entries.TryGetValue(KeyOf(g), out var e) ? (e.Kills, g) : (null, g);

        var rows = new List<WeaponSkillRow>();
        foreach (var (kills, grant) in entries.Values)
        {
            var record = kills ?? grant!;

            Item? template = record.TemplateGuid.HasValue
                ? await _items.GetItemByGuidAsync(record.TemplateGuid.Value)
                : await _items.GetItemByIdAsync(record.TemplateId);

            var skillEffects = (template?.WeaponEffects ?? new List<WeaponEffectType>())
                .Where(WeaponProficiencyTracker.NeedsWeaponSkill).ToList();

            // A weapon with no skills (a Dagger, a Bow) has nothing to unlock here. Its kills still count toward its damage type.
            if (template != null && skillEffects.Count == 0) continue;

            bool granted = grant != null;
            int count = kills?.Kills ?? 0;
            bool unlocked = granted || count >= Max;
            string icon = unlocked ? "✅" : "🔒";

            var killRecord = kills;
            rows.Add(new WeaponSkillRow
            {
                Name = record.Name,
                StatusText = granted ? "Warrior weapon pick"
                    : unlocked ? "Unlocked"
                    : $"{Max} kills with this weapon to unlock",
                HasKills = killRecord != null,
                KillsText = $"{count} / {Max}",
                NoteText = template == null ? "This weapon's data isn't available (it may have been deleted)." : string.Empty,
                Skills = skillEffects.Select(e =>
                {
                    string title = $"{icon} {EnumDisplayHelper.GetName(e)}";
                    string description = EnumDisplayHelper.GetEffectDescription(e);
                    string state = unlocked
                        ? "✅ Unlocked."
                        : $"🔒 Locked until {Max} kills with this weapon (you have {count}).";
                    return new WeaponSkillInfo
                    {
                        Title = title,
                        Description = description,
                        ExplainCommand = new Command(async () =>
                            await Application.Current!.MainPage!.DisplayAlertAsync(
                                EnumDisplayHelper.GetName(e), $"{description}\n\n{state}", "OK"))
                    };
                }).ToList(),
                UpCommand = new Command(async () =>
                {
                    if (killRecord == null) return;
                    await _tracker.SetWeaponKillsAsync(killRecord, killRecord.Kills + 1);
                    await RefreshAsync();
                }),
                DownCommand = new Command(async () =>
                {
                    if (killRecord == null) return;
                    await _tracker.SetWeaponKillsAsync(killRecord, killRecord.Kills - 1);
                    await RefreshAsync();
                })
            });
        }

        WeaponRows.Clear();
        foreach (var row in rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)) WeaponRows.Add(row);

        OnPropertyChanged(nameof(HasNoWeapons));
        OnPropertyChanged(nameof(NeedsStartingChoice));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}