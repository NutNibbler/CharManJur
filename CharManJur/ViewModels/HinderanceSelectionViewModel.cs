using CharManJur.Models;
using CharManJur.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels;

public class HinderanceSelectionViewModel : INotifyPropertyChanged
{
    private readonly IHinderanceDataService _hinderanceDataService;
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;

    private ObservableCollection<Hinderance> _hinderances = new();
    private Hinderance? _selectedHinderance;
    private bool _isLoading;
    private HinderanceRewardType _selectedRewardType = HinderanceRewardType.None;
    private string? _selectedStat;
    private List<string> _availableStats = new();
    private bool _isConfirming = false;

    public ObservableCollection<Hinderance> Hinderances
    {
        get => _hinderances;
        set
        {
            _hinderances = value;
            OnPropertyChanged();
        }
    }

    public Hinderance? SelectedHinderance
    {
        get => _selectedHinderance;
        set
        {
            if (_selectedHinderance != value)
            {
                // Clear previous hinderance effects
                if (_selectedHinderance != null)
                {
                    _charDataService.HinderanceVigorPenalty = 0;
                    _charDataService.HinderanceAgilityPenalty = 0;
                    _charDataService.HinderanceMindPenalty = 0;
                    _charDataService.HinderanceSpiritPenalty = 0;

                    // Reset training points when changing hinderance
                    ResetTrainingPoints();
                }

                _selectedHinderance = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedHinderance));
                OnPropertyChanged(nameof(SelectedHinderanceName));
                OnPropertyChanged(nameof(SelectedHinderanceDescription));

                // Only reset rewards if NOT in the middle of confirming
                if (!_isConfirming)
                {
                    SelectedRewardType = HinderanceRewardType.None;
                    SelectedStat = null;
                }

                // Update the data service
                _charDataService.SelectedHinderance = value;
            }
        }
    }

    private void ResetTrainingPoints()
    {
        // Reset all skill training levels to -2 (base penalty, no training points)
        foreach (var skill in _charDataService.SkillTrainingLevels.Keys.ToList())
        {
            _charDataService.SkillTrainingLevels[skill] = 0;
        }

        // Reset AvailableTrainingPoints to 4 (base)
        _charDataService.AvailableTrainingPoints = 4;

        System.Diagnostics.Debug.WriteLine("=== Training points reset due to hinderance change ===");
    }

    private void ClearRewards()
    {
        // Only clear if not in the middle of confirming
        if (!_isConfirming)
        {
            _charDataService.SelectedRewardType = HinderanceRewardType.None;
            _charDataService.RewardStatBonusAmount = 0;
            _charDataService.RewardStatName = null;
            _charDataService.SelectedRewardSkillName = null;
        }
    }

    public bool HasSelectedHinderance => SelectedHinderance != null;
    public string SelectedHinderanceName => SelectedHinderance?.Name ?? "No Hinderance Selected";
    public string SelectedHinderanceDescription => SelectedHinderance?.Description ?? string.Empty;

    public HinderanceRewardType SelectedRewardType
    {
        get => _selectedRewardType;
        set
        {
            if (_selectedRewardType != value)
            {
                _selectedRewardType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStatBonusSelected));
                OnPropertyChanged(nameof(IsTrainingPointSelected));
                OnPropertyChanged(nameof(RewardSummary));
                _charDataService.SelectedRewardType = value;

                // Clear stat selection if switching away from StatBonus
                if (value != HinderanceRewardType.StatBonus)
                {
                    SelectedStat = null;
                }
            }
        }
    }

    public bool IsStatBonusSelected => SelectedRewardType == HinderanceRewardType.StatBonus;
    public bool IsTrainingPointSelected => SelectedRewardType == HinderanceRewardType.TrainingPoint;

    public string? SelectedStat
    {
        get => _selectedStat;
        set
        {
            if (_selectedStat != value)
            {
                _selectedStat = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RewardSummary));
                if (value != null)
                {
                    _charDataService.RewardStatBonusAmount = 1;
                    _charDataService.RewardStatName = value;
                }
            }
        }
    }

    public List<string> AvailableStats
    {
        get => _availableStats;
        set
        {
            _availableStats = value;
            OnPropertyChanged();
        }
    }

    public string RewardSummary
    {
        get
        {
            if (!HasSelectedHinderance) return "Select a hinderance to see available rewards.";

            if (SelectedRewardType == HinderanceRewardType.None)
                return "Select a reward type (Stat Bonus or Training Point).";

            if (SelectedRewardType == HinderanceRewardType.StatBonus)
            {
                return string.IsNullOrEmpty(SelectedStat)
                    ? "Choose a stat to increase."
                    : $"Reward: +1 {SelectedStat}";
            }

            if (SelectedRewardType == HinderanceRewardType.TrainingPoint)
            {
                return "Reward: +1 Training Point (available on Training page)";
            }

            return "Select a reward.";
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadHinderancesCommand { get; }
    public ICommand SelectStatBonusCommand { get; }
    public ICommand SelectTrainingPointCommand { get; }
    public ICommand SelectStatToIncreaseCommand { get; }
    public ICommand ConfirmHinderanceCommand { get; }
    public ICommand SkipHinderanceCommand { get; }

    public HinderanceSelectionViewModel(
        IHinderanceDataService hinderanceDataService,
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService)
    {
        _hinderanceDataService = hinderanceDataService;
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;

        LoadHinderancesCommand = new Command(async () => await LoadHinderancesAsync());
        SelectStatBonusCommand = new Command(() => SelectedRewardType = HinderanceRewardType.StatBonus);
        SelectTrainingPointCommand = new Command(() => SelectedRewardType = HinderanceRewardType.TrainingPoint);
        SelectStatToIncreaseCommand = new Command<string>(stat => SelectedStat = stat);
        ConfirmHinderanceCommand = new Command(async () => await ConfirmHinderanceAsync());
        SkipHinderanceCommand = new Command(async () => await SkipHinderanceAsync());

        LoadStats();
        Task.Run(LoadHinderancesAsync);
    }

    private void LoadStats()
    {
        AvailableStats = new List<string> { "Vigor", "Agility", "Mind", "Spirit" };
    }

    private async Task LoadHinderancesAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            var hinderances = await _hinderanceDataService.GetHinderancesAsync();
            Hinderances = new ObservableCollection<Hinderance>(hinderances);

            // Restore selected hinderance from service if exists
            if (_charDataService.SelectedHinderance != null)
            {
                SelectedHinderance = Hinderances.FirstOrDefault(h => h.Id == _charDataService.SelectedHinderance.Id);
                SelectedRewardType = _charDataService.SelectedRewardType;
                SelectedStat = _charDataService.RewardStatName;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading hinderances: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ConfirmHinderanceAsync()
    {
        if (SelectedHinderance == null)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "No Hinderance Selected",
                "Please select a hinderance, or use the 'Skip' button to proceed without one.",
                "OK");
            return;
        }

        if (SelectedRewardType == HinderanceRewardType.None)
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "No Reward Selected",
                "Please select a reward for your hinderance, or use the 'Skip' button.",
                "OK");
            return;
        }

        if (SelectedRewardType == HinderanceRewardType.StatBonus && string.IsNullOrEmpty(SelectedStat))
        {
            await Application.Current.MainPage.DisplayAlertAsync(
                "No Stat Selected",
                "Please select a stat to increase.",
                "OK");
            return;
        }

        // Set flag to prevent reward clearing
        _isConfirming = true;

        try
        {
            // Clear any previous rewards first
            _charDataService.SelectedRewardType = HinderanceRewardType.None;
            _charDataService.RewardStatBonusAmount = 0;
            _charDataService.RewardStatName = null;
            _charDataService.SelectedRewardSkillName = null;

            // Reset training points to base if we're applying a new hinderance
            ResetTrainingPoints();

            // Store the selected hinderance in the service
            _charDataService.SelectedHinderance = SelectedHinderance;

            // Set penalty properties for display
            _charDataService.HinderanceVigorPenalty = Math.Abs(SelectedHinderance.VigorModifier);
            _charDataService.HinderanceAgilityPenalty = Math.Abs(SelectedHinderance.AgilityModifier);
            _charDataService.HinderanceMindPenalty = Math.Abs(SelectedHinderance.MindModifier);
            _charDataService.HinderanceSpiritPenalty = Math.Abs(SelectedHinderance.SpiritModifier);

            // Apply hinderance reward
            if (SelectedRewardType == HinderanceRewardType.StatBonus && !string.IsNullOrEmpty(SelectedStat))
            {
                _charDataService.SelectedRewardType = HinderanceRewardType.StatBonus;
                _charDataService.RewardStatBonusAmount = 1;
                _charDataService.RewardStatName = SelectedStat;
                _charDataService.SelectedRewardSkillName = null;

                System.Diagnostics.Debug.WriteLine($"=== Hinderance Stat Bonus: +1 {SelectedStat} ===");
                System.Diagnostics.Debug.WriteLine($"=== SelectedRewardType: {_charDataService.SelectedRewardType} ===");
                System.Diagnostics.Debug.WriteLine($"=== RewardStatName: {_charDataService.RewardStatName} ===");
                System.Diagnostics.Debug.WriteLine($"=== RewardStatBonusAmount: {_charDataService.RewardStatBonusAmount} ===");
            }
            else if (SelectedRewardType == HinderanceRewardType.TrainingPoint)
            {
                _charDataService.SelectedRewardType = HinderanceRewardType.TrainingPoint;
                _charDataService.AvailableTrainingPoints += 1;
                _charDataService.RewardStatBonusAmount = 0;
                _charDataService.RewardStatName = null;
                _charDataService.SelectedRewardSkillName = null;

                System.Diagnostics.Debug.WriteLine($"=== Hinderance Training Point: +1 Available Training Point ===");
                System.Diagnostics.Debug.WriteLine($"=== SelectedRewardType: {_charDataService.SelectedRewardType} ===");
                System.Diagnostics.Debug.WriteLine($"=== Total Training Points Available: {_charDataService.AvailableTrainingPoints} ===");
            }

            // Force recalculation of stats
            _charDataService.ASMStatVigor = _charDataService.GetAbilityModifier(_charDataService.TotalStatVigor);
            _charDataService.ASMStatAgility = _charDataService.GetAbilityModifier(_charDataService.TotalStatAgility);
            _charDataService.ASMStatMind = _charDataService.GetAbilityModifier(_charDataService.TotalStatMind);
            _charDataService.ASMStatSpirit = _charDataService.GetAbilityModifier(_charDataService.TotalStatSpirit);

            await Shell.Current.GoToAsync("///Godrick_Training_Popup");
        }
        finally
        {
            // Clear the flag after navigation
            _isConfirming = false;
        }
    }

    private async Task SkipHinderanceAsync()
    {
        // Clear all hinderance effects
        _charDataService.HinderanceVigorPenalty = 0;
        _charDataService.HinderanceAgilityPenalty = 0;
        _charDataService.HinderanceMindPenalty = 0;
        _charDataService.HinderanceSpiritPenalty = 0;

        // Clear any selected hinderance data
        _charDataService.SelectedHinderance = null;
        _charDataService.SelectedRewardType = HinderanceRewardType.None;
        _charDataService.SelectedRewardSkillName = null;
        _charDataService.RewardStatBonusAmount = 0;
        _charDataService.RewardStatName = null;

        // Reset training points to base (4)
        _charDataService.AvailableTrainingPoints = 4;

        // Reset the ViewModel properties too
        SelectedRewardType = HinderanceRewardType.None;
        SelectedStat = null;

        // Navigate to Training Popup
        await Shell.Current.GoToAsync("///Godrick_Training_Popup");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}