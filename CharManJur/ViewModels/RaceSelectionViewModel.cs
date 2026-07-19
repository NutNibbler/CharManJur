using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class RaceSelectionViewModel : INotifyPropertyChanged
{
    private readonly IRaceDataService _raceDataService;
    private readonly ICharAttribDataService _charDataService;

    private ObservableCollection<Race> _races = new();
    private Race? _selectedRace;
    private bool _isLoading;

    public ObservableCollection<Race> Races
    {
        get => _races;
        set
        {
            _races = value;
            OnPropertyChanged();
        }
    }

    public Race? SelectedRace
    {
        get => _selectedRace;
        set
        {
            if (_selectedRace != value)
            {
                _selectedRace = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedRace));
                OnPropertyChanged(nameof(SelectedRaceName));
                OnPropertyChanged(nameof(SelectedRaceDescription));
                OnPropertyChanged(nameof(SelectedRaceFeatureName));
                OnPropertyChanged(nameof(SelectedRaceFeatureDescription));
                OnPropertyChanged(nameof(SelectedRaceHpBonus));
                OnPropertyChanged(nameof(SelectedRaceVigorBonus));
                OnPropertyChanged(nameof(SelectedRaceAgilityBonus));
                OnPropertyChanged(nameof(SelectedRaceMindBonus));
                OnPropertyChanged(nameof(SelectedRaceSpiritBonus));
                OnPropertyChanged(nameof(SelectedRaceSkillBonuses));
                OnPropertyChanged(nameof(HasAbilityBonuses));
                OnPropertyChanged(nameof(HasSkillBonuses));
                OnPropertyChanged(nameof(HasAnyBonuses));
            }
        }
    }

    public bool HasSelectedRace => SelectedRace != null;
    public string SelectedRaceName => SelectedRace?.Name ?? "Select a Race";
    public string SelectedRaceDescription => SelectedRace?.Description ?? "Description will appear here";
    public string SelectedRaceFeatureName => SelectedRace?.FeatureName ?? "Features will appear here";
    public string SelectedRaceFeatureDescription => SelectedRace?.FeatureDescription ?? "Features will appear here";
    public int? SelectedRaceHpBonus => SelectedRace?.HitProtectionBonus ?? null;

    // ===== RACE BONUS DISPLAY PROPERTIES =====
    public string SelectedRaceVigorBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            var value = SelectedRace.VigorModifier;
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }

    public string SelectedRaceAgilityBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            var value = SelectedRace.AgilityModifier;
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }

    public string SelectedRaceMindBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            var value = SelectedRace.MindModifier;
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }

    public string SelectedRaceSpiritBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            var value = SelectedRace.SpiritModifier;
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }

    public string SelectedRaceSkillBonuses
    {
        get
        {
            if (SelectedRace?.SkillBonuses == null || !SelectedRace.SkillBonuses.Any())
                return "No skill bonuses";

            return string.Join(", ", SelectedRace.SkillBonuses.Select(s =>
            {
                var bonus = s.Bonus;
                var bonusDisplay = bonus > 0 ? $"+{bonus}" : bonus.ToString();
                return $"{s.SkillName} {bonusDisplay}";
            }));
        }
    }

    public bool HasAbilityBonuses
    {
        get
        {
            if (SelectedRace == null) return false;
            return SelectedRace.VigorModifier != 0 ||
                   SelectedRace.AgilityModifier != 0 ||
                   SelectedRace.MindModifier != 0 ||
                   SelectedRace.SpiritModifier != 0;
        }
    }

    public bool HasSkillBonuses
    {
        get
        {
            if (SelectedRace?.SkillBonuses == null) return false;
            return SelectedRace.SkillBonuses.Any();
        }
    }

    public bool HasAnyBonuses => HasAbilityBonuses || HasSkillBonuses;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadRacesCommand { get; }
    public ICommand SelectRaceCommand { get; }
    public ICommand ConfirmRaceCommand { get; }
    public ICommand CreateCustomRaceCommand { get; }
    public ICommand EditCustomRaceCommand { get; }
    public ICommand DeleteCustomRaceCommand { get; }

    public RaceSelectionViewModel(
        IRaceDataService raceDataService,
        ICharAttribDataService charDataService)
    {
        _raceDataService = raceDataService;
        _charDataService = charDataService;

        LoadRacesCommand = new Command(async () => await LoadRacesAsync());
        SelectRaceCommand = new Command<Race>(OnRaceSelected);
        ConfirmRaceCommand = new Command(async () => await ConfirmRaceAsync());
        CreateCustomRaceCommand = new Command(async () => await CreateCustomRaceAsync());
        EditCustomRaceCommand = new Command<Race>(async (race) => await EditCustomRaceAsync(race));
        DeleteCustomRaceCommand = new Command<Race>(async (race) => await DeleteCustomRaceAsync(race));

        Task.Run(LoadRacesAsync);
    }

    private async Task CreateCustomRaceAsync()
    {
        // Use the route without "///" if registered without it
        await Shell.Current.GoToAsync("///Godrick_CustomRaceCreator");
    }

    public async Task EditCustomRaceAsync(Race? race)
    {
        if (race == null) return;

        // Only allow editing custom races (ID >= 90001)
        if (race.Id < 90001)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Cannot Edit",
                "Foundation races cannot be edited.",
                "OK");
            return;
        }

        // Navigate to editor with race data
        var navigationParameters = new Dictionary<string, object>
    {
        { "RaceToEdit", race }
    };
        await Shell.Current.GoToAsync("///Godrick_CustomRaceCreator", navigationParameters);
    }

    public async Task DeleteCustomRaceAsync(Race? race)
    {
        if (race == null) return;

        // Only allow deleting custom races (ID >= 90001)
        if (race.Id < 90001)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Cannot Delete",
                "Foundation races cannot be deleted.",
                "OK");
            return;
        }

        var confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Delete Custom Kin",
            $"Are you sure you want to delete '{race.Name}'? This action cannot be undone.",
            "Yes, Delete",
            "No, Cancel");

        if (!confirm) return;

        try
        {
            var customRaceStorage = Application.Current.Handler?.MauiContext?.Services?.GetService<ICustomRaceStorageService>();
            if (customRaceStorage != null)
            {
                var deleted = await customRaceStorage.DeleteCustomRaceAsync(race.Id);
                if (deleted)
                {
                    // Remove from the observable collection
                    Races.Remove(race);

                    // If this was the selected race, clear selection
                    if (SelectedRace?.Id == race.Id)
                    {
                        SelectedRace = null;
                    }

                    await Application.Current.MainPage.DisplayAlertAsync(
                        "Success",
                        $"Kin '{race.Name}' has been deleted.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Error",
                $"Failed to delete race: {ex.Message}",
                "OK");
        }
    }

    private async Task LoadRacesAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            // CHANGE THIS: Use GetAllRacesAsync to include custom races
            var races = await _raceDataService.GetAllRacesAsync();

            // Filter races based on campaign type
            var filteredRaces = races.Where(r => r.CompatibleCampaigns.Contains(_charDataService.CampaignType)).ToList();

            Races = new ObservableCollection<Race>(filteredRaces);

            // Restore selected race from saved data
            RestoreFromSavedData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading races: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnRaceSelected(Race? race)
    {
        SelectedRace = race;
    }

    public bool SelectRaceByName(string raceName)
    {
        if (string.IsNullOrEmpty(raceName))
        {
            ResetSelection();
            return false;
        }

        var race = Races.FirstOrDefault(r => r.Name == raceName);
        if (race != null)
        {
            SelectedRace = race;
            return true;
        }

        ResetSelection();
        return false;
    }

    public void ResetSelection()
    {
        System.Diagnostics.Debug.WriteLine("=== RaceSelectionViewModel: ResetSelection() START ===");

        _selectedRace = null;
        OnPropertyChanged(nameof(SelectedRace));
        OnPropertyChanged(nameof(HasSelectedRace));
        OnPropertyChanged(nameof(SelectedRaceName));
        OnPropertyChanged(nameof(SelectedRaceDescription));
        OnPropertyChanged(nameof(SelectedRaceFeatureName));
        OnPropertyChanged(nameof(SelectedRaceFeatureDescription));
        OnPropertyChanged(nameof(Races));

        System.Diagnostics.Debug.WriteLine("=== RaceSelectionViewModel: ResetSelection() COMPLETE ===");
    }

    public void RestoreFromSavedData()
    {
        System.Diagnostics.Debug.WriteLine("=== RaceSelectionViewModel: RestoreFromSavedData() ===");

        // First try to restore by ID
        if (_charDataService.SelectedRaceId.HasValue)
        {
            var race = Races.FirstOrDefault(r => r.Id == _charDataService.SelectedRaceId.Value);
            if (race != null)
            {
                SelectedRace = race;
                System.Diagnostics.Debug.WriteLine($"Restored race by ID: {race.Name}");
                return;
            }
        }

        // Fallback: restore by name
        if (!string.IsNullOrEmpty(_charDataService.CharacterRace))
        {
            var race = Races.FirstOrDefault(r => r.Name == _charDataService.CharacterRace);
            if (race != null)
            {
                SelectedRace = race;
                System.Diagnostics.Debug.WriteLine($"Restored race by name: {race.Name}");
                return;
            }
        }

        System.Diagnostics.Debug.WriteLine("No race found to restore");
        ResetSelection();
    }

    private async Task ConfirmRaceAsync()
    {
        if (SelectedRace == null)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "No Kin Selected",
                "Please select a kin to continue.",
                "OK");
            return;
        }

        // ===== STORE RACE DATA IN SERVICE =====
        _charDataService.CharacterRace = SelectedRace.Name;
        _charDataService.CharacterRaceDescription = SelectedRace.Description;
        _charDataService.CharacterRaceFeatureName = SelectedRace.FeatureName;
        _charDataService.CharacterRaceFeatureDescription = SelectedRace.FeatureDescription;
        _charDataService.SelectedRaceId = SelectedRace.Id;

        // ===== APPLY RACE BONUSES =====
        _charDataService.RaceVigorBonus = SelectedRace.VigorModifier;
        _charDataService.RaceAgilityBonus = SelectedRace.AgilityModifier;
        _charDataService.RaceMindBonus = SelectedRace.MindModifier;
        _charDataService.RaceSpiritBonus = SelectedRace.SpiritModifier;
        _charDataService.RaceSkillBonuses = SelectedRace.SkillBonuses?.ToList() ?? new List<RaceSkillBonus>();

        // ===== APPLY PREHENSILE LIMBS =====
        _charDataService.LimbSets = SelectedRace.LimbSets?.ToList() ?? new List<PrehensileLimbSet>();

        System.Diagnostics.Debug.WriteLine($"=== Applied Race: {SelectedRace.Name} ===");
        System.Diagnostics.Debug.WriteLine($"  VigorModifier: {SelectedRace.VigorModifier}");
        System.Diagnostics.Debug.WriteLine($"  AgilityModifier: {SelectedRace.AgilityModifier}");
        System.Diagnostics.Debug.WriteLine($"  MindModifier: {SelectedRace.MindModifier}");
        System.Diagnostics.Debug.WriteLine($"  SpiritModifier: {SelectedRace.SpiritModifier}");
        System.Diagnostics.Debug.WriteLine($"  SkillBonuses: {SelectedRace.SkillBonuses?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"  LimbSets: {SelectedRace.LimbSets?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"  Total Limb Slots: {_charDataService.GetTotalLimbSlots()}");

        System.Diagnostics.Debug.WriteLine($"=== Applied Race: {SelectedRace.Name} ===");
        System.Diagnostics.Debug.WriteLine($"  VigorModifier: {SelectedRace.VigorModifier}");
        System.Diagnostics.Debug.WriteLine($"  AgilityModifier: {SelectedRace.AgilityModifier}");
        System.Diagnostics.Debug.WriteLine($"  MindModifier: {SelectedRace.MindModifier}");
        System.Diagnostics.Debug.WriteLine($"  SpiritModifier: {SelectedRace.SpiritModifier}");
        System.Diagnostics.Debug.WriteLine($"  SkillBonuses: {SelectedRace.SkillBonuses?.Count ?? 0}");

        // Navigate to Class Selection
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_ClassSelection");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}