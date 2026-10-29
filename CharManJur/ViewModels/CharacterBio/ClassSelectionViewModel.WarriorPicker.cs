using System.Collections.ObjectModel;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// The Warrior's additional weapon, picked in a panel inside the class selection screen.
public partial class ClassSelectionViewModel
{
    private List<WeaponChoice> _pickerAll = new();
    private ObservableCollection<WeaponChoice> _pickerWeapons = new();
    private WeaponChoice? _selectedPickerWeapon;
    private string _pickerSearch = string.Empty;
    private bool _filtering;
    private int _pickerLoadToken;

    public bool IsWarriorSelected => SelectedClass?.ClassName == "Warrior";

    public ObservableCollection<WeaponChoice> PickerWeapons
    {
        get => _pickerWeapons;
        set { _pickerWeapons = value; OnPropertyChanged(); }
    }

    public bool PickerIsEmpty => PickerWeapons.Count == 0;

    public WeaponChoice? SelectedPickerWeapon
    {
        get => _selectedPickerWeapon;
        set
        {
            if (_filtering && value == null) return;   // the list was rebuilt, not a deselection
            if (ReferenceEquals(_selectedPickerWeapon, value)) return;

            _selectedPickerWeapon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPickerSelection));
            OnPropertyChanged(nameof(PickerSelectionText));
            OnPropertyChanged(nameof(CanSelectClass));
            OnPropertyChanged(nameof(SelectClassButtonText));
            (ConfirmClassSelectionCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool HasPickerSelection => SelectedPickerWeapon != null;

    public string PickerSelectionText => SelectedPickerWeapon == null
        ? "Choose a weapon to continue."
        : $"Chosen: {SelectedPickerWeapon.Name}";

    public string PickerSearch
    {
        get => _pickerSearch;
        set
        {
            if (_pickerSearch == value) return;
            _pickerSearch = value ?? string.Empty;
            OnPropertyChanged();
            ApplyPickerFilter();
        }
    }

    // Called whenever the selected class changes: loads the list for a Warrior, clears it otherwise.
    private async Task RefreshWarriorPickerAsync()
    {
        OnPropertyChanged(nameof(IsWarriorSelected));
        OnPropertyChanged(nameof(CanSelectClass));
        OnPropertyChanged(nameof(SelectClassButtonText));

        if (!IsWarriorSelected)
        {
            ClearWarriorPicker();
            return;
        }

        int token = ++_pickerLoadToken;

        var items = _serviceProvider.GetRequiredService<IItemDataService>();
        var state = _charDataService.Proficiency;

        // Picks made elsewhere can't be chosen again. This creation pick may be re-chosen.
        var others = state.GrantedSkills.Where(r => !r.FromCreation).ToList();
        var loaded = await WeaponChoices.LoadAsync(items, others);
        if (token != _pickerLoadToken) return;   // the class changed while loading

        _pickerAll = loaded;
        _pickerSearch = string.Empty;
        OnPropertyChanged(nameof(PickerSearch));
        ApplyPickerFilter();

        // Going back and forward reopens the panel on the earlier pick.
        var current = state.CreationGrant;
        SelectedPickerWeapon = current == null ? null : _pickerAll.FirstOrDefault(c => current.IsFor(c.Item));
    }

    private void ApplyPickerFilter()
    {
        string term = _pickerSearch.Trim();
        var shown = term.Length == 0
            ? _pickerAll
            : _pickerAll.Where(c => c.SearchText.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        _filtering = true;
        try { PickerWeapons = new ObservableCollection<WeaponChoice>(shown); }
        finally { _filtering = false; }

        OnPropertyChanged(nameof(SelectedPickerWeapon));   // lets the list re-select the chosen weapon
        OnPropertyChanged(nameof(PickerIsEmpty));
    }

    private void ClearWarriorPicker()
    {
        _pickerLoadToken++;
        _pickerAll = new List<WeaponChoice>();
        _selectedPickerWeapon = null;
        _pickerSearch = string.Empty;

        _filtering = true;
        try { PickerWeapons = new ObservableCollection<WeaponChoice>(); }
        finally { _filtering = false; }

        OnPropertyChanged(nameof(SelectedPickerWeapon));
        OnPropertyChanged(nameof(HasPickerSelection));
        OnPropertyChanged(nameof(PickerSelectionText));
        OnPropertyChanged(nameof(PickerSearch));
        OnPropertyChanged(nameof(PickerIsEmpty));
        OnPropertyChanged(nameof(CanSelectClass));
        OnPropertyChanged(nameof(SelectClassButtonText));
        (ConfirmClassSelectionCommand as Command)?.ChangeCanExecute();
    }

    private async Task ProceedToBackgroundAsync()
    {
        SaveClassDataToService();
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_BackgroundSelection", true);
    }
}