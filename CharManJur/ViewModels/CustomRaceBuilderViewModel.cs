using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class CustomRaceBuilderViewModel : INotifyPropertyChanged
{
    private readonly ICustomRaceStorageService _customRaceStorage;
    private readonly ICharAttribDataService _charDataService;

    // ===== RACE PROPERTIES =====
    private string _raceName = string.Empty;
    private string _description = string.Empty;
    private string _featureName = string.Empty;
    private string _featureDescription = string.Empty;
    private int _vigorModifier = 0;
    private int _agilityModifier = 0;
    private int _mindModifier = 0;
    private int _spiritModifier = 0;

    // ===== SKILL BONUSES =====
    private ObservableCollection<RaceSkillBonus> _skillBonuses = new();
    private string _selectedSkillName = string.Empty;
    private int _selectedSkillBonus = 1;

    // ===== LIMB SETS =====
    private ObservableCollection<PrehensileLimbSet> _limbSets = new();
    private string _limbSetName = string.Empty;
    private LimbPairType _selectedLimbPairType = LimbPairType.Paired;
    private string _limbSetIcon = "🫳";
    private string _leftSlotName = "Left";
    private string _rightSlotName = "Right";

    // ===== EDIT STATE =====
    private Race? _editingRace;
    private bool _isEditMode;
    private int _nextLimbSetId = 1;

    // ===== PREDEFINED SKILLS =====
    public List<string> PredefinedSkills { get; } = new()
    {
        "Athletics", "Acrobatics", "Aim", "Arcana", "Artifice",
        "Commune", "Constitution", "Deception", "Diplomacy", "Drive",
        "Grapple", "Heal", "Investigate", "Lore", "Sight",
        "Presence", "Ride", "Stealth", "Survival", "Thief"
    };

    public ObservableCollection<LimbPairType> LimbPairTypes { get; } = new()
    {
        LimbPairType.Paired,
        LimbPairType.Single
    };

    // ===== PROPERTIES =====
    public string RaceName
    {
        get => _raceName;
        set { _raceName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public string FeatureName
    {
        get => _featureName;
        set { _featureName = value; OnPropertyChanged(); }
    }

    public string FeatureDescription
    {
        get => _featureDescription;
        set { _featureDescription = value; OnPropertyChanged(); }
    }

    public int VigorModifier
    {
        get => _vigorModifier;
        set { _vigorModifier = value; OnPropertyChanged(); }
    }

    public int AgilityModifier
    {
        get => _agilityModifier;
        set { _agilityModifier = value; OnPropertyChanged(); }
    }

    public int MindModifier
    {
        get => _mindModifier;
        set { _mindModifier = value; OnPropertyChanged(); }
    }

    public int SpiritModifier
    {
        get => _spiritModifier;
        set { _spiritModifier = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RaceSkillBonus> SkillBonuses
    {
        get => _skillBonuses;
        set { _skillBonuses = value; OnPropertyChanged(); }
    }

    public string SelectedSkillName
    {
        get => _selectedSkillName;
        set { _selectedSkillName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddSkill)); }
    }

    public int SelectedSkillBonus
    {
        get => _selectedSkillBonus;
        set { _selectedSkillBonus = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddSkill)); }
    }

    public ObservableCollection<PrehensileLimbSet> LimbSets
    {
        get => _limbSets;
        set { _limbSets = value; OnPropertyChanged(); }
    }

    public string LimbSetName
    {
        get => _limbSetName;
        set { _limbSetName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddLimbSet)); }
    }

    public LimbPairType SelectedLimbPairType
    {
        get => _selectedLimbPairType;
        set
        {
            _selectedLimbPairType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPairedType));
        }
    }

    public string LimbSetIcon
    {
        get => _limbSetIcon;
        set { _limbSetIcon = value; OnPropertyChanged(); }
    }

    public string LeftSlotName
    {
        get => _leftSlotName;
        set { _leftSlotName = value; OnPropertyChanged(); }
    }

    public string RightSlotName
    {
        get => _rightSlotName;
        set { _rightSlotName = value; OnPropertyChanged(); }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set { _isEditMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); OnPropertyChanged(nameof(SaveButtonText)); }
    }

    public string PageTitle => IsEditMode ? "Edit Custom Kin" : "Create Custom Kin";
    public string SaveButtonText => IsEditMode ? "Update Kin" : "Create Kin";

    public bool CanSave => !string.IsNullOrWhiteSpace(RaceName);
    public bool CanAddSkill => !string.IsNullOrWhiteSpace(SelectedSkillName) && SelectedSkillBonus != 0;
    public bool CanAddLimbSet => !string.IsNullOrWhiteSpace(LimbSetName);

    // ===== COMMANDS =====
    public ICommand AddSkillCommand { get; }
    public ICommand RemoveSkillCommand { get; }
    public ICommand AddLimbSetCommand { get; }
    public ICommand RemoveLimbSetCommand { get; }
    public ICommand SaveRaceCommand { get; }
    public ICommand CancelCommand { get; }

    public CustomRaceBuilderViewModel(
        ICustomRaceStorageService customRaceStorage,
        ICharAttribDataService charDataService)
    {
        _customRaceStorage = customRaceStorage;
        _charDataService = charDataService;

        AddSkillCommand = new Command(AddSkill);
        RemoveSkillCommand = new Command<RaceSkillBonus>(RemoveSkill);
        AddLimbSetCommand = new Command(AddLimbSet);
        RemoveLimbSetCommand = new Command<PrehensileLimbSet>(RemoveLimbSet);
        SaveRaceCommand = new Command(async () => await SaveRaceAsync());
        CancelCommand = new Command(async () => await CancelAsync());

        // Add default limb set
        AddDefaultLimbSet();
    }

    public void LoadRaceForEdit(Race race)
    {
        _editingRace = race;
        IsEditMode = true;

        RaceName = race.Name;
        Description = race.Description;
        FeatureName = race.FeatureName;
        FeatureDescription = race.FeatureDescription;
        VigorModifier = race.VigorModifier;
        AgilityModifier = race.AgilityModifier;
        MindModifier = race.MindModifier;
        SpiritModifier = race.SpiritModifier;

        // Load skill bonuses
        SkillBonuses.Clear();
        foreach (var bonus in race.SkillBonuses)
        {
            SkillBonuses.Add(new RaceSkillBonus { SkillName = bonus.SkillName, Bonus = bonus.Bonus });
        }

        // Load limb sets
        LimbSets.Clear();
        _nextLimbSetId = 1;
        foreach (var limbSet in race.LimbSets)
        {
            LimbSets.Add(new PrehensileLimbSet
            {
                Id = _nextLimbSetId++,
                Name = limbSet.Name,
                PairType = limbSet.PairType,
                SlotCount = limbSet.SlotCount,
                DisplayName = limbSet.DisplayName,
                Icon = limbSet.Icon,
                LeftSlotName = limbSet.LeftSlotName,
                RightSlotName = limbSet.RightSlotName,
                SlotIndices = limbSet.SlotIndices.ToList(),
                TwoHandedItemId = limbSet.TwoHandedItemId
            });
        }

        // If no limb sets, add default
        if (!LimbSets.Any())
        {
            AddDefaultLimbSet();
        }
    }

    public bool IsPairedType => SelectedLimbPairType == LimbPairType.Paired;

    // Update SelectedLimbPairType setter to notify IsPairedType changes
    

    private void AddDefaultLimbSet()
    {
        var slotCount = LimbSets.Count * 2;
        LimbSets.Add(new PrehensileLimbSet
        {
            Id = _nextLimbSetId++,
            Name = "Hands",
            PairType = LimbPairType.Paired,
            SlotCount = 2,
            DisplayName = "Hands",
            Icon = "🫳",
            LeftSlotName = "Left",
            RightSlotName = "Right",
            SlotIndices = new List<int> { slotCount, slotCount + 1 }
        });
    }

    private void AddSkill()
    {
        if (!CanAddSkill) return;

        if (SkillBonuses.Any(s => s.SkillName == SelectedSkillName))
        {
            // Skill already exists, update it
            var existing = SkillBonuses.First(s => s.SkillName == SelectedSkillName);
            existing.Bonus = SelectedSkillBonus;
        }
        else
        {
            SkillBonuses.Add(new RaceSkillBonus
            {
                SkillName = SelectedSkillName,
                Bonus = SelectedSkillBonus
            });
        }

        // Reset selection
        SelectedSkillName = string.Empty;
        SelectedSkillBonus = 1;
    }

    private void RemoveSkill(RaceSkillBonus? bonus)
    {
        if (bonus == null) return;
        SkillBonuses.Remove(bonus);
    }

    private void AddLimbSet()
    {
        if (!CanAddLimbSet) return;

        var slotCount = LimbSets.Count * 2;
        var newSet = new PrehensileLimbSet
        {
            Id = _nextLimbSetId++,
            Name = LimbSetName,
            PairType = SelectedLimbPairType,
            SlotCount = SelectedLimbPairType == LimbPairType.Paired ? 2 : 1,
            DisplayName = LimbSetName,
            Icon = string.IsNullOrWhiteSpace(LimbSetIcon) ? "🫳" : LimbSetIcon,
            LeftSlotName = SelectedLimbPairType == LimbPairType.Paired ? LeftSlotName : string.Empty,
            RightSlotName = SelectedLimbPairType == LimbPairType.Paired ? RightSlotName : string.Empty,
            SlotIndices = new List<int>()
        };

        // Assign slot indices
        if (SelectedLimbPairType == LimbPairType.Paired)
        {
            newSet.SlotIndices = new List<int> { slotCount, slotCount + 1 };
        }
        else
        {
            newSet.SlotIndices = new List<int> { slotCount };
        }

        LimbSets.Add(newSet);

        // Reset fields
        LimbSetName = string.Empty;
        LimbSetIcon = "🫳";
        LeftSlotName = "Left";
        RightSlotName = "Right";
    }

    private void RemoveLimbSet(PrehensileLimbSet? limbSet)
    {
        if (limbSet == null) return;
        if (LimbSets.Count <= 1)
        {
            // Don't allow removing the last limb set
            Application.Current.MainPage.DisplayAlertAsync("Cannot Remove",
                "A race must have at least one limb set.", "OK");
            return;
        }
        LimbSets.Remove(limbSet);
    }

    private async Task SaveRaceAsync()
    {
        if (!CanSave) return;

        // Validate
        if (!LimbSets.Any())
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Missing Limb Sets",
                "Please add at least one limb set to the race.",
                "OK");
            return;
        }

        var race = new Race
        {
            Id = IsEditMode && _editingRace != null
                ? _editingRace.Id
                : await _customRaceStorage.GetNextCustomRaceIdAsync(),
            RaceNameId = $"custom_{RaceName.Replace(" ", "").Replace("'", "").Replace("-", "")}",
            Name = RaceName,
            Description = Description,
            FeatureName = FeatureName,
            FeatureDescription = FeatureDescription,
            VigorModifier = VigorModifier,
            AgilityModifier = AgilityModifier,
            MindModifier = MindModifier,
            SpiritModifier = SpiritModifier,
            SkillBonuses = SkillBonuses.ToList(),
            LimbSets = LimbSets.ToList(),
            CompatibleCampaigns = new List<string> { "Godrick" },
            HitProtectionBonus = 0
        };

        try
        {
            await _customRaceStorage.SaveCustomRaceAsync(race);

            string message = IsEditMode
                ? $"Kin '{RaceName}' has been updated successfully!"
                : $"Kin '{RaceName}' has been created successfully!";

            await Application.Current.MainPage.DisplayAlertAsync("Success", message, "OK");

            // Navigate back to Kin Selection
            await NavigateBackToKinSelection();
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Error",
                $"Failed to save race: {ex.Message}",
                "OK");
        }
    }

    private async Task CancelAsync()
    {
        if (HasUnsavedChanges())
        {
            var confirm = await Application.Current.MainPage.DisplayAlertAsync(
                "Unsaved Changes",
                "You have unsaved changes. Are you sure you want to leave?",
                "Yes, Leave",
                "No, Stay");

            if (!confirm) return;
        }

        await NavigateBackToKinSelection();
    }

    private async Task NavigateBackToKinSelection()
    {
        try
        {
            // Try to pop the page if it was pushed
            if (Application.Current.MainPage is NavigationPage navPage)
            {
                if (navPage.Navigation.NavigationStack.Count > 1)
                {
                    await navPage.Navigation.PopAsync();
                    return;
                }
            }

            // Navigate back using absolute route
            await Shell.Current.GoToAsync("//CharBuilder_Godrick_KinSelection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            // Last resort - try simple absolute navigation
            try
            {
                await Shell.Current.GoToAsync("///CharBuilder_Godrick_KinSelection");
            }
            catch (Exception innerEx)
            {
                System.Diagnostics.Debug.WriteLine($"Fallback navigation error: {innerEx.Message}");
                // Final fallback - use the current shell
                await Shell.Current.GoToAsync("//CharBuilder_Godrick_KinSelection");
            }
        }
    }

    private bool HasUnsavedChanges()
    {
        // Simple check - if any field has content, consider it unsaved
        return !string.IsNullOrWhiteSpace(RaceName) ||
               !string.IsNullOrWhiteSpace(Description) ||
               !string.IsNullOrWhiteSpace(FeatureName) ||
               !string.IsNullOrWhiteSpace(FeatureDescription) ||
               SkillBonuses.Any() ||
               (LimbSets.Count > 1) || // More than the default single set
               VigorModifier != 0 ||
               AgilityModifier != 0 ||
               MindModifier != 0 ||
               SpiritModifier != 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}