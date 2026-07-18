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
            return SelectedRace.VigorModifier != 0
                ? $"+{SelectedRace.VigorModifier}"
                : "0";
        }
    }

    public string SelectedRaceAgilityBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            return SelectedRace.AgilityModifier != 0
                ? $"+{SelectedRace.AgilityModifier}"
                : "0";
        }
    }

    public string SelectedRaceMindBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            return SelectedRace.MindModifier != 0
                ? $"+{SelectedRace.MindModifier}"
                : "0";
        }
    }

    public string SelectedRaceSpiritBonus
    {
        get
        {
            if (SelectedRace == null) return "0";
            return SelectedRace.SpiritModifier != 0
                ? $"+{SelectedRace.SpiritModifier}"
                : "0";
        }
    }

    public string SelectedRaceSkillBonuses
    {
        get
        {
            if (SelectedRace?.SkillBonuses == null || !SelectedRace.SkillBonuses.Any())
                return "No skill bonuses";

            return string.Join(", ", SelectedRace.SkillBonuses.Select(s => $"{s.SkillName} +{s.Bonus}"));
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

    public RaceSelectionViewModel(
        IRaceDataService raceDataService,
        ICharAttribDataService charDataService)
    {
        _raceDataService = raceDataService;
        _charDataService = charDataService;

        LoadRacesCommand = new Command(async () => await LoadRacesAsync());
        SelectRaceCommand = new Command<Race>(OnRaceSelected);
        ConfirmRaceCommand = new Command(async () => await ConfirmRaceAsync());

        Task.Run(LoadRacesAsync);
    }

    private async Task LoadRacesAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var races = await _raceDataService.GetRacesAsync();

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