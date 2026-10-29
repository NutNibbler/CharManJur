using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;

namespace CharManJur.ViewModels;

// One choice in the Starting Damage Type Proficiency panel. Type == null means "Pick later".
public class StartingProficiencyOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public WeaponDamageType? Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ICommand SelectCommand { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// The starting damage type proficiency, picked (or deliberately postponed) on the class selection screen.
public partial class ClassSelectionViewModel
{
    private StartingProficiencyOption? _startingChoice;
    private Guid _startingChoiceFor;   // the character the choice belongs to, so a loaded character never inherits it

    public ObservableCollection<StartingProficiencyOption> StartingOptions { get; } = new();

    public bool HasStartingChoice => _startingChoice != null;

    private void InitStartingOptions()
    {
        void Add(WeaponDamageType? type, string title, string description)
        {
            var option = new StartingProficiencyOption { Type = type, Title = title, Description = description };
            option.SelectCommand = new Command(() => SelectStartingOption(option));
            StartingOptions.Add(option);
        }

        Add(WeaponDamageType.Piercing, "Piercing", "A maximum damage roll ignores armor.");
        Add(WeaponDamageType.Bludgeoning, "Bludgeoning", "Always ignores 1 armor.");
        Add(WeaponDamageType.Slashing, "Slashing", "+1 damage against targets with 0 armor.");
        Add(null, "Pick later", "Decide on the dashboard or the Bio Sheet. You start with no damage type proficiency until then.");
    }

    private void SelectStartingOption(StartingProficiencyOption? option)
    {
        _startingChoice = option;
        _startingChoiceFor = _charDataService.CharacterGuid;

        foreach (var o in StartingOptions) o.IsSelected = ReferenceEquals(o, option);

        OnPropertyChanged(nameof(HasStartingChoice));
        OnPropertyChanged(nameof(CanSelectClass));
        OnPropertyChanged(nameof(SelectClassButtonText));
        (ConfirmClassSelectionCommand as Command)?.ChangeCanExecute();
    }

    private void ClearStartingChoice() => SelectStartingOption(null);

    // Reopening the screen (back/forward, or a loaded character) shows the saved choice, or none.
    private void RestoreStartingChoice()
    {
        var state = _charDataService.Proficiency;
        var granted = state.DamageTypes.FirstOrDefault(r => r.Granted);

        if (state.Initialized && granted != null)
        {
            SelectStartingOption(StartingOptions.FirstOrDefault(o => o.Type == granted.Type));
        }
        else if (_startingChoice != null && _startingChoiceFor != _charDataService.CharacterGuid)
        {
            ClearStartingChoice();   // left over from another character
        }
    }

    // Writes the choice into the character. "Pick later" leaves the state uninitialized, so the dashboard asks as before.
    private void ApplyStartingChoice()
    {
        var state = _charDataService.Proficiency;

        foreach (var row in state.DamageTypes) row.Granted = false;

        if (_startingChoice?.Type is WeaponDamageType type)
        {
            var row = state.DamageTypes.FirstOrDefault(r => r.Type == type);
            if (row == null)
            {
                row = new DamageTypeProgress { Type = type };
                state.DamageTypes.Add(row);
            }

            row.Granted = true;
            state.Initialized = true;
        }
        else
        {
            state.Initialized = false;
        }
    }
}