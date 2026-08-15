using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;

namespace CharManJur.ViewModels;

public class ClassSelectionViewModel : INotifyPropertyChanged
{
    private readonly IClassDataService _classDataService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICharAttribDataService _charDataService;

    private ObservableCollection<CharacterClass> _classes = new();
    private CharacterClass? _selectedClass;
    private ClassFeature? _selectedFeature;
    private bool _isLoading;

    private bool _isSubFeatureRequired;
    private bool _hasSelectedSubFeature;
    private bool _hasFeatures;
    private bool _isShifter;

    private List<SubFeatureType> _availableUnlockTypes = new();

    public bool HasFeatures
    {
        get => _hasFeatures;
        set
        {
            if (_hasFeatures != value)
            {
                _hasFeatures = value;
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"HasFeatures changed to: {value}");
            }
        }
    }

    public bool IsShifter
    {
        get => _isShifter;
        set
        {
            if (_isShifter != value)
            {
                _isShifter = value;
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"IsShifter changed to: {value}");
            }
        }
    }

    public ObservableCollection<CharacterClass> Classes
    {
        get => _classes;
        set
        {
            _classes = value;
            OnPropertyChanged();
        }
    }

    public CharacterClass? SelectedClass
    {
        get => _selectedClass;
        set
        {
            if (_selectedClass != value)
            {
                // Clear sub-features when class changes
                if (_selectedClass != null && value != null && _selectedClass.Id != value.Id)
                {
                    ClearSubFeatures();
                }

                _selectedClass = value;
                OnPropertyChanged();

                RefreshSelectedClassFeatures();
                
                // Auto-select first feature when class changes
                if (_selectedClass != null && _selectedClass.Features.Count > 0)
                {
                    SelectedFeature = _selectedClass.Features[0];
                }
                else
                {
                    SelectedFeature = null;
                }
                
                // Update UI bindings
                OnPropertyChanged(nameof(HasSelectedClass));
                OnPropertyChanged(nameof(SelectedClassName));
                OnPropertyChanged(nameof(SelectedClassDescription));
                OnPropertyChanged(nameof(SelectedClassFeatures));
                OnPropertyChanged(nameof(SelectedClassLevelingSkillBonus));
                OnPropertyChanged(nameof(SelectedClassRecurringBenefit));
                OnPropertyChanged(nameof(SelectedClassHitpointsBenefit));

                // Update unlockable types
                UpdateAvailableUnlockTypes();
                OnPropertyChanged(nameof(HasAnyUnlockableTypes));
                OnPropertyChanged(nameof(CanUnlockQuips));
                OnPropertyChanged(nameof(CanUnlockSpells));
                OnPropertyChanged(nameof(CanUnlockTechniques));
                OnPropertyChanged(nameof(CanUnlockBlueprints));
            }
        }
    }

    public ClassFeature? SelectedFeature
    {
        get => _selectedFeature;
        set
        {
            if (_selectedFeature != value)
            {
                // Clear sub-features when feature changes
                if (_selectedFeature != null && value != null && _selectedFeature.Id != value.Id)
                {
                    ClearSubFeatures();
                }

                _selectedFeature = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedFeature));
                OnPropertyChanged(nameof(SelectedFeatureName));
                OnPropertyChanged(nameof(SelectedFeatureDescription));

                // Update unlockable types
                UpdateAvailableUnlockTypes();
                OnPropertyChanged(nameof(HasAnyUnlockableTypes));
                OnPropertyChanged(nameof(CanUnlockQuips));
                OnPropertyChanged(nameof(CanUnlockSpells));
                OnPropertyChanged(nameof(CanUnlockTechniques));
                OnPropertyChanged(nameof(CanUnlockBlueprints));
            }
        }
    }

    public bool HasSelectedClass => SelectedClass != null;
    public string SelectedClassName => SelectedClass?.ClassName ?? "Select a Class";
    public string SelectedClassDescription => SelectedClass?.Description ?? "Description will appear here, if it exists";
    public string SelectedClassLevelingSkillBonus => SelectedClass?.LevelingSkillBonus ?? "No allocation requirement";
    public string SelectedClassRecurringBenefit => SelectedClass?.RecurringBenefit ?? "No recurring benefit available";

    public string SelectedClassHitpointsBenefit => "Hitpoints Bonus: " + (SelectedClass?.HitProtectionBonus.ToString() ?? "No Hitpoints bonus available.");

    private ObservableCollection<ClassFeature> _selectedClassFeatures = new();
    public ObservableCollection<ClassFeature> SelectedClassFeatures
    {
        get => _selectedClassFeatures;
        set
        {
            _selectedClassFeatures = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedFeature => SelectedFeature != null;
    public string SelectedFeatureName => SelectedFeature?.Name ?? "Select a Feature";
    public string SelectedFeatureDescription => SelectedFeature?.Description ?? "Feature description will appear here";

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    // === SUB-FEATURE UNLOCK PROPERTIES ===

    private void UpdateAvailableUnlockTypes()
    {
        var types = new List<SubFeatureType>();

        if (SelectedClass != null)
        {
            types.AddRange(SelectedClass.ClassUnlockableTypes);
        }

        if (SelectedFeature != null)
        {
            types.AddRange(SelectedFeature.UnlockableTypes);
        }

        _availableUnlockTypes = types.Distinct().ToList();

        // Update sub-feature requirement status
        IsSubFeatureRequired = HasAnyUnlockableTypes;

        // Reset sub-feature selection status when class/feature changes
        if (IsSubFeatureRequired)
        {
            CheckSubFeatureSelection();
        }

        // Update button state
        OnPropertyChanged(nameof(CanSelectClass));
        OnPropertyChanged(nameof(SelectClassButtonText));

        // Refresh selected sub-feature names
        RefreshSelectedNames();
    }

    private void RefreshSelectedClassFeatures()
    {
        if (SelectedClass != null)
        {
            SelectedClassFeatures = new ObservableCollection<ClassFeature>(SelectedClass.Features);
            HasFeatures = SelectedClassFeatures.Any();
        }
        else
        {
            SelectedClassFeatures = new ObservableCollection<ClassFeature>();
            HasFeatures = SelectedClassFeatures.Any();
        }
        OnPropertyChanged(nameof(SelectedClassFeatures));
    }

    public bool HasAnyUnlockableTypes => _availableUnlockTypes.Any();
    public bool CanUnlockQuips => _availableUnlockTypes.Contains(SubFeatureType.Quip);
    public bool CanUnlockSpells => _availableUnlockTypes.Contains(SubFeatureType.Spell);
    public bool CanUnlockTechniques => _availableUnlockTypes.Contains(SubFeatureType.Technique);
    public bool CanUnlockBlueprints => _availableUnlockTypes.Contains(SubFeatureType.Blueprint);

    // === SUB-FEATURE UNLOCK COMMANDS ===
    public ICommand UnlockQuipsCommand { get; }
    public ICommand UnlockSpellsCommand { get; }
    public ICommand UnlockTechniquesCommand { get; }
    public ICommand UnlockBlueprintsCommand { get; }

    // === EXISTING COMMANDS ===
    public ICommand LoadClassesCommand { get; }
    public ICommand SelectClassCommand { get; }
    public ICommand SelectFeatureCommand { get; }
    public ICommand ConfirmClassSelectionCommand { get; }
    public ICommand ClearAllSubFeaturesCommand { get; }

    // === SUB-FEATURE REQUIREMENT PROPERTIES ===
    public bool IsSubFeatureRequired
    {
        get => _isSubFeatureRequired;
        set
        {
            _isSubFeatureRequired = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedSubFeature
    {
        get => _hasSelectedSubFeature;
        set
        {
            _hasSelectedSubFeature = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSelectClass));
            OnPropertyChanged(nameof(SelectClassButtonText));
        }
    }

    public bool CanSelectClass =>
        !IsSubFeatureRequired || HasSelectedSubFeature;

    public string SelectClassButtonText =>
        IsSubFeatureRequired && !HasSelectedSubFeature
            ? "Select Sub-Feature First"
            : "Select This Class";

    // === SELECTED SUB-FEATURE NAMES ===
    public string SelectedBlueprintNames =>
        string.Join(", ", _charDataService.AcquiredBlueprints.Select(b => b.BlueprintName));

    public string SelectedQuipNames =>
        string.Join(", ", _charDataService.AcquiredQuips.Select(q => q.QuipName));

    public string SelectedSpellNames =>
        string.Join(", ", _charDataService.AcquiredSpells.Select(s => s.SpellName));

    public string SelectedTechniqueNames =>
        string.Join(", ", _charDataService.AcquiredTechniques.Select(t => t.TechniqueName));

    private void RefreshSelectedNames()
    {
        OnPropertyChanged(nameof(SelectedBlueprintNames));
        OnPropertyChanged(nameof(SelectedQuipNames));
        OnPropertyChanged(nameof(SelectedSpellNames));
        OnPropertyChanged(nameof(SelectedTechniqueNames));
    }

    // === CONSTRUCTOR ===
    public ClassSelectionViewModel(
        IClassDataService classDataService,
        IServiceProvider serviceProvider,
        ICharAttribDataService charDataService)
    {
        _classDataService = classDataService;
        _serviceProvider = serviceProvider;
        _charDataService = charDataService;

        LoadClassesCommand = new Command(async () => await LoadClassesAsync());
        SelectClassCommand = new Command<CharacterClass>(OnClassSelected);
        SelectFeatureCommand = new Command<ClassFeature>(OnFeatureSelected);

        // Sub-feature unlock commands
        UnlockQuipsCommand = new Command(OnUnlockQuips, () => CanUnlockQuips);
        UnlockSpellsCommand = new Command(OnUnlockSpells, () => CanUnlockSpells);
        UnlockTechniquesCommand = new Command(OnUnlockTechniques, () => CanUnlockTechniques);
        UnlockBlueprintsCommand = new Command(OnUnlockBlueprints, () => CanUnlockBlueprints);

        // Confirm class selection command
        ConfirmClassSelectionCommand = new Command(async () => await ConfirmClassSelectionAsync(), () => CanSelectClass);

        // Clear all sub-features command
        ClearAllSubFeaturesCommand = new Command(ClearAllSubFeatures, () => HasAnyUnlockableTypes);

        Task.Run(LoadClassesAsync);
    }

    private async Task LoadClassesAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var classes = await _classDataService.GetClassesAsync();
            Classes = new ObservableCollection<CharacterClass>(classes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading classes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnClassSelected(CharacterClass? selectedClass)
    {
        SelectedClass = selectedClass;
        System.Diagnostics.Debug.WriteLine("IS SHIFTER TRIGGER");
        if (selectedClass != null && selectedClass.ClassName == "Shifter")
        {
            IsShifter = true;
        }
        else
        {
            IsShifter = false;
        }
    }

    private void OnFeatureSelected(ClassFeature? selectedFeature)
    {
        SelectedFeature = selectedFeature;
    }

    // === SUB-FEATURE UNLOCK HANDLERS ===

    private async void OnUnlockQuips()
    {
        await NavigateToSubFeatureSelection(SubFeatureType.Quip);
        CheckSubFeatureSelection();
    }

    private async void OnUnlockSpells()
    {
        await NavigateToSubFeatureSelection(SubFeatureType.Spell);
        CheckSubFeatureSelection();
    }

    private async void OnUnlockTechniques()
    {
        await NavigateToSubFeatureSelection(SubFeatureType.Technique);
        CheckSubFeatureSelection();
    }

    private async void OnUnlockBlueprints()
    {
        await NavigateToSubFeatureSelection(SubFeatureType.Blueprint);
        CheckSubFeatureSelection();
    }

    private void CheckSubFeatureSelection()
    {
        // Check if any sub-features have been acquired
        bool hasQuips = _charDataService.AcquiredQuips.Any();
        bool hasSpells = _charDataService.AcquiredSpells.Any();
        bool hasTechniques = _charDataService.AcquiredTechniques.Any();
        bool hasBlueprints = _charDataService.AcquiredBlueprints.Any();

        HasSelectedSubFeature = hasQuips || hasSpells || hasTechniques || hasBlueprints;

        // Force update of all selected names
        RefreshSelectedNames();
        OnPropertyChanged(nameof(HasSelectedSubFeature));
    }

    // === CLEAR SUB-FEATURES ===

    public void ClearSubFeatures()
    {
        _charDataService.AcquiredBlueprints.Clear();
        _charDataService.AcquiredQuips.Clear();
        _charDataService.AcquiredSpells.Clear();
        _charDataService.AcquiredTechniques.Clear();
        HasSelectedSubFeature = false;
        RefreshSelectedNames();
    }

    private void ClearAllSubFeatures()
    {
        // Clear all acquired sub-features from the service
        _charDataService.AcquiredBlueprints.Clear();
        _charDataService.AcquiredQuips.Clear();
        _charDataService.AcquiredSpells.Clear();
        _charDataService.AcquiredTechniques.Clear();

        // Reset the selection flag
        HasSelectedSubFeature = false;

        // Refresh the UI
        RefreshSelectedNames();
        OnPropertyChanged(nameof(HasSelectedSubFeature));
        OnPropertyChanged(nameof(CanSelectClass));
        OnPropertyChanged(nameof(SelectClassButtonText));
    }

    // === HANDLE CLASS SELECTION ===

    public async Task ConfirmClassSelectionAsync()
    {
        // Check if sub-feature is required but not selected
        if (IsSubFeatureRequired && !HasSelectedSubFeature)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "Sub-Feature Required",
                $"You must select at least one sub-feature for {SelectedClass?.ClassName} before proceeding.",
                "OK");
            return;
        }

        // Save class and feature data to global service
        SaveClassDataToService();

        // Navigate to next page
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_BackgroundSelection", true);
    }

    private void SaveClassDataToService()
    {
        if (SelectedClass == null) return;

        // Store class ID
        _charDataService.SelectedClassId = SelectedClass.Id;

        // Store the single selected feature for now (can be expanded later)
        if (SelectedFeature != null)
        {
            _charDataService.AddFeature(SelectedFeature.Id);
        }

        // Also store display strings for immediate UI display
        _charDataService.CharacterClassName = SelectedClass.ClassName;
        _charDataService.CharacterClassDescription = SelectedClass.Description;
        _charDataService.CharacterClassFeatureName = SelectedFeature?.Name ?? "None";
        _charDataService.CharacterClassFeatureDescription = SelectedFeature?.Description ?? "None";
        _charDataService.LevelUpAllocationRequirement = SelectedClass.LevelingSkillBonus;
        _charDataService.CharacterRecurringBenefit = SelectedClass.RecurringBenefit;
    }

    // === NAVIGATION ===

    private async Task NavigateToSubFeatureSelection(SubFeatureType type)
    {
        try
        {
            var currentPage = Application.Current.MainPage;
            var viewModel = _serviceProvider.GetRequiredService<SubFeatureSelectionViewModel>();

            var popup = new CharBuilder_Godrick_SubFeatureSelectionPopup(
                viewModel,
                type,
                SelectedClass?.Id ?? 0,
                SelectedFeature?.Id ?? 0
            );

            // Subscribe to the popup's event
            popup.SubFeaturesSelected += OnSubFeaturesSelected;

            await currentPage.Navigation.PushModalAsync(popup);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== NAVIGATION ERROR: {ex.Message} ===");
            await Application.Current.MainPage.DisplayAlertAsync("Error", $"Navigation failed: {ex.Message}", "OK");
        }
    }

    public event EventHandler? SubFeatureSelectionCompleted;

    // Update the OnSubFeaturesSelected method
    private void OnSubFeaturesSelected(object? sender, EventArgs e)
    {
        // Refresh the sub-feature selection status and names
        CheckSubFeatureSelection();

        // Notify the page that we're returning from the popup
        SubFeatureSelectionCompleted?.Invoke(this, EventArgs.Empty);
    }

    // === RESET SELECTION (For clearing when cancelling) ===

    public void ResetSelection()
    {
        System.Diagnostics.Debug.WriteLine("=== ClassSelectionViewModel: ResetSelection() START ===");

        SelectedClass = null;
        SelectedFeature = null;
        _availableUnlockTypes = new List<SubFeatureType>();
        HasSelectedSubFeature = false;
        IsSubFeatureRequired = false;

        _charDataService.AcquiredBlueprints.Clear();
        _charDataService.AcquiredQuips.Clear();
        _charDataService.AcquiredSpells.Clear();
        _charDataService.AcquiredTechniques.Clear();

        SelectedClassFeatures = new ObservableCollection<ClassFeature>();

        // FORCE a new collection instance for Classes
        var currentClasses = _classes;
        _classes = new ObservableCollection<CharacterClass>();
        OnPropertyChanged(nameof(Classes));
        _classes = currentClasses;
        OnPropertyChanged(nameof(Classes));

        // Force SelectedClassFeatures to refresh
        OnPropertyChanged(nameof(SelectedClassFeatures));

        OnPropertyChanged(nameof(HasSelectedClass));
        OnPropertyChanged(nameof(SelectedClassName));
        OnPropertyChanged(nameof(SelectedClassDescription));
        OnPropertyChanged(nameof(SelectedClassLevelingSkillBonus));
        OnPropertyChanged(nameof(SelectedClassRecurringBenefit));
        OnPropertyChanged(nameof(HasAnyUnlockableTypes));
        OnPropertyChanged(nameof(CanSelectClass));
        OnPropertyChanged(nameof(SelectClassButtonText));
        OnPropertyChanged(nameof(SelectedBlueprintNames));
        OnPropertyChanged(nameof(SelectedQuipNames));
        OnPropertyChanged(nameof(SelectedSpellNames));
        OnPropertyChanged(nameof(SelectedTechniqueNames));

        System.Diagnostics.Debug.WriteLine("=== ClassSelectionViewModel: ResetSelection() COMPLETE ===");
    }

    public void ClearSelections()
    {
        ResetSelection();
    }

    // === CLEAR SELECTIONS (Alias for ResetSelection, for compatibility) ===

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void RestoreFromSavedData()
    {
        System.Diagnostics.Debug.WriteLine("=== ClassSelectionViewModel: RestoreFromSavedData() ===");

        // First try to restore by ID
        if (_charDataService.SelectedClassId.HasValue)
        {
            var classToSelect = Classes.FirstOrDefault(c => c.Id == _charDataService.SelectedClassId.Value);
            if (classToSelect != null)
            {
                SelectedClass = classToSelect;
                System.Diagnostics.Debug.WriteLine($"Restored class by ID: {classToSelect.ClassName}");

                // Restore feature if one is saved
                if (!string.IsNullOrEmpty(_charDataService.CharacterClassFeatureName))
                {
                    var featureToSelect = classToSelect.Features.FirstOrDefault(f => f.Name == _charDataService.CharacterClassFeatureName);
                    if (featureToSelect != null)
                    {
                        SelectedFeature = featureToSelect;
                        System.Diagnostics.Debug.WriteLine($"Restored feature: {featureToSelect.Name}");
                    }
                }
                return;
            }
        }

        // Fallback: restore by name
        if (!string.IsNullOrEmpty(_charDataService.CharacterClassName))
        {
            var classToSelect = Classes.FirstOrDefault(c => c.ClassName == _charDataService.CharacterClassName);
            if (classToSelect != null)
            {
                SelectedClass = classToSelect;
                System.Diagnostics.Debug.WriteLine($"Restored class by name: {classToSelect.ClassName}");

                if (!string.IsNullOrEmpty(_charDataService.CharacterClassFeatureName))
                {
                    var featureToSelect = classToSelect.Features.FirstOrDefault(f => f.Name == _charDataService.CharacterClassFeatureName);
                    if (featureToSelect != null)
                    {
                        SelectedFeature = featureToSelect;
                        System.Diagnostics.Debug.WriteLine($"Restored feature: {featureToSelect.Name}");
                    }
                }
                return;
            }
        }

        // Clear selections if no class found
        SelectedClass = null;
        SelectedFeature = null;
        RefreshSelectedClassFeatures();
        System.Diagnostics.Debug.WriteLine("No class found to restore");
    }

}