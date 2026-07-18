using CharManJur.Models;
using CharManJur.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace CharManJur.ViewModels;

public class TrainingPopupViewModel : INotifyPropertyChanged
{
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharacterPersistenceService _persistenceService;

    // === SKILL DATA ===
    public Dictionary<string, int> SkillTrainingLevels { get; set; } = new();

    // === STAT BREAKDOWN PROPERTIES ===
    public string VigorDisplay => GetStatDisplay("Vigor", _charDataService.StatVigor ?? 10, _charDataService.BGVigorBonus, _charDataService.RaceVigorBonus, _charDataService.HinderanceVigorPenalty, _charDataService.RewardStatName == "Vigor" ? _charDataService.RewardStatBonusAmount : 0);
    public string AgilityDisplay => GetStatDisplay("Agility", _charDataService.StatAgility ?? 10, _charDataService.BGAgilityBonus, _charDataService.RaceAgilityBonus, _charDataService.HinderanceAgilityPenalty, _charDataService.RewardStatName == "Agility" ? _charDataService.RewardStatBonusAmount : 0);
    public string MindDisplay => GetStatDisplay("Mind", _charDataService.StatMind ?? 10, _charDataService.BGMindBonus, _charDataService.RaceMindBonus, _charDataService.HinderanceMindPenalty, _charDataService.RewardStatName == "Mind" ? _charDataService.RewardStatBonusAmount : 0);
    public string SpiritDisplay => GetStatDisplay("Spirit", _charDataService.StatSpirit ?? 10, _charDataService.BGSpiritBonus, _charDataService.RaceSpiritBonus, _charDataService.HinderanceSpiritPenalty, _charDataService.RewardStatName == "Spirit" ? _charDataService.RewardStatBonusAmount : 0);

    // === TOTAL STATS ===
    public int TotalStatVigor => _charDataService.TotalStatVigor;
    public int TotalStatAgility => _charDataService.TotalStatAgility;
    public int TotalStatMind => _charDataService.TotalStatMind;
    public int TotalStatSpirit => _charDataService.TotalStatSpirit;

    public int TotalASMStatVigor => _charDataService.TotalASMStatVigor;
    public int TotalASMStatAgility => _charDataService.TotalASMStatAgility;
    public int TotalASMStatMind => _charDataService.TotalASMStatMind;
    public int TotalASMStatSpirit => _charDataService.TotalASMStatSpirit;

    // === TRAINING POINTS ===
    private const int BASE_TRAINING_POINTS = 4;
    private int _usedPoints = 0;

    public int TotalAvailablePoints => 4 + (_charDataService.AvailableTrainingPoints - 4);
    public int AvailablePoints => TotalAvailablePoints - _usedPoints;
    public bool HasAvailablePoints => AvailablePoints > 0;

    // === MODE ===
    public bool IsInCharacterCreation => _globalMenuDataService.IsInCharacterCreation;

    // === COMMANDS ===
    public event PropertyChangedEventHandler? PropertyChanged;

    public TrainingPopupViewModel(
    ICharAttribDataService charDataService,
    IGlobalMenuDataService globalMenuDataService,
    ICharacterPersistenceService persistenceService)
    {
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
        _persistenceService = persistenceService;

        // Initialize skill training levels from service
        foreach (var skill in GetSkillNames())
        {
            if (_charDataService.SkillTrainingLevels.ContainsKey(skill))
            {
                int value = _charDataService.SkillTrainingLevels[skill];
                // If value is negative, it's old format, convert to training points
                if (value < 0)
                {
                    // Old format stored -2 + training points, so training points = value + 2
                    value = value + 2;
                    if (value < 0) value = 0;
                    // Update the service with the corrected value
                    _charDataService.SkillTrainingLevels[skill] = value;
                }
                SkillTrainingLevels[skill] = value;
            }
            else
            {
                SkillTrainingLevels[skill] = 0;
            }
        }

        CalculateUsedPoints();
    }

    public List<string> GetSkillNames()
    {
        return new List<string>
        {
            "Athletics", "Constitution", "Grapple", "Presence", "Ride",
            "Acrobatics", "Aim", "Drive", "Stealth", "Thief",
            "Arcana", "Artifice", "Heal", "Investigate", "Lore",
            "Commune", "Deception", "Diplomacy", "Sight", "Survival"
        };
    }

    public int GetStatBonusForSkill(string skillName)
    {
        return skillName switch
        {
            "Athletics" => TotalASMStatVigor,
            "Constitution" => TotalASMStatVigor,
            "Grapple" => TotalASMStatVigor,
            "Presence" => TotalASMStatVigor,
            "Ride" => TotalASMStatVigor,
            "Acrobatics" => TotalASMStatAgility,
            "Aim" => TotalASMStatAgility,
            "Drive" => TotalASMStatAgility,
            "Stealth" => TotalASMStatAgility,
            "Thief" => TotalASMStatAgility,
            "Arcana" => TotalASMStatMind,
            "Artifice" => TotalASMStatMind,
            "Heal" => TotalASMStatMind,
            "Investigate" => TotalASMStatMind,
            "Lore" => TotalASMStatMind,
            "Commune" => TotalASMStatSpirit,
            "Deception" => TotalASMStatSpirit,
            "Diplomacy" => TotalASMStatSpirit,
            "Sight" => TotalASMStatSpirit,
            "Survival" => TotalASMStatSpirit,
            _ => 0
        };
    }

    // ===== NEW: Get Combined Creation Bonus (Background + Race) =====
    public int GetCreationBonusForSkill(string skillName)
    {
        int totalBonus = 0;

        // Background bonus
        if (_charDataService.SelectedSkillBonuses != null)
        {
            var bgBonus = _charDataService.SelectedSkillBonuses
                .FirstOrDefault(b => b.SkillName == skillName);
            totalBonus += bgBonus?.Bonus ?? 0;
        }

        // Race bonus
        if (_charDataService.RaceSkillBonuses != null)
        {
            var raceBonus = _charDataService.RaceSkillBonuses
                .FirstOrDefault(b => b.SkillName == skillName);
            totalBonus += raceBonus?.Bonus ?? 0;
        }

        return totalBonus;
    }

    public int GetSkillTotal(string skillName)
    {
        int trainingPoints = SkillTrainingLevels.ContainsKey(skillName) ? SkillTrainingLevels[skillName] : 0;
        int statBonus = GetStatBonusForSkill(skillName);
        int creationBonus = GetCreationBonusForSkill(skillName);
        int basePenalty = -2;  // Always applied
        return basePenalty + trainingPoints + statBonus + creationBonus;
    }

    public void UpdateTrainingLevel(string skillName, int value)
    {
        if (SkillTrainingLevels.ContainsKey(skillName))
        {
            int oldValue = SkillTrainingLevels[skillName];
            int difference = value - oldValue;

            // Check if we have enough points available
            if (_usedPoints + difference > TotalAvailablePoints)
            {
                System.Diagnostics.Debug.WriteLine($"Not enough points! Used: {_usedPoints}, Available: {TotalAvailablePoints}, Trying to add: {difference}");
                return;
            }

            SkillTrainingLevels[skillName] = value;
            _usedPoints += difference;

            // Store ONLY the training points
            if (_charDataService.SkillTrainingLevels.ContainsKey(skillName))
            {
                _charDataService.SkillTrainingLevels[skillName] = value;
            }

            OnPropertyChanged(nameof(AvailablePoints));
            OnPropertyChanged(nameof(HasAvailablePoints));
            OnPropertyChanged(nameof(TotalASMStatVigor));
            OnPropertyChanged(nameof(TotalASMStatAgility));
            OnPropertyChanged(nameof(TotalASMStatMind));
            OnPropertyChanged(nameof(TotalASMStatSpirit));

            // Update skill total
            OnPropertyChanged($"SkillTotal_{skillName}");

            System.Diagnostics.Debug.WriteLine($"UpdateTrainingLevel: {skillName} = {value}, Used: {_usedPoints}, Available: {AvailablePoints}");
        }
    }

    public void RecalculateAvailablePoints()
    {
        System.Diagnostics.Debug.WriteLine($"=== RecalculateAvailablePoints called ===");
        System.Diagnostics.Debug.WriteLine($"AvailableTrainingPoints: {_charDataService.AvailableTrainingPoints}");

        // Reset the training levels based on the current service state
        foreach (var skill in GetSkillNames())
        {
            if (_charDataService.SkillTrainingLevels.ContainsKey(skill))
            {
                int value = _charDataService.SkillTrainingLevels[skill];
                // If value is negative, it's old format, convert
                if (value < 0)
                {
                    value = value + 2;
                    if (value < 0) value = 0;
                    _charDataService.SkillTrainingLevels[skill] = value;
                }
                SkillTrainingLevels[skill] = value;
                System.Diagnostics.Debug.WriteLine($"  {skill}: trainingPoints={SkillTrainingLevels[skill]}");
            }
            else
            {
                SkillTrainingLevels[skill] = 0;
                System.Diagnostics.Debug.WriteLine($"  {skill}: not found, setting to 0");
            }
        }

        CalculateUsedPoints();
        OnPropertyChanged(nameof(AvailablePoints));
        OnPropertyChanged(nameof(HasAvailablePoints));
        OnPropertyChanged(nameof(TotalAvailablePoints));

        System.Diagnostics.Debug.WriteLine($"TotalAvailablePoints: {TotalAvailablePoints}");
        System.Diagnostics.Debug.WriteLine($"UsedPoints: {_usedPoints}");
        System.Diagnostics.Debug.WriteLine($"AvailablePoints: {AvailablePoints}");
    }

    public void CalculateUsedPoints()
    {
        _usedPoints = 0;
        foreach (var value in SkillTrainingLevels.Values)
        {
            _usedPoints += value;
        }
        OnPropertyChanged(nameof(AvailablePoints));
        OnPropertyChanged(nameof(HasAvailablePoints));
    }

    private string GetStatDisplay(string statName, int baseValue, int backgroundBonus, int raceBonus, int hinderancePenalty, int hinderanceReward)
    {
        int total = baseValue + backgroundBonus + raceBonus - hinderancePenalty + hinderanceReward;

        var parts = new List<string>();
        parts.Add(baseValue.ToString()); // Always show the rolled/base value first

        if (backgroundBonus != 0)
            parts.Add($"{backgroundBonus:+0;-0} Background");

        if (raceBonus != 0)
            parts.Add($"{raceBonus:+0;-0} Race");

        if (hinderancePenalty != 0)
            parts.Add($"-{hinderancePenalty} Hinderance");

        if (hinderanceReward != 0)
            parts.Add($"+{hinderanceReward} Reward");

        if (parts.Count == 1)
            return $"{statName}: {total}";

        return $"{statName}: {total} ({string.Join(", ", parts)})";
    }

    public async Task SaveCharacterAsync()
    {
        // Save training points (0-4) directly
        foreach (var kvp in SkillTrainingLevels)
        {
            if (_charDataService.SkillTrainingLevels.ContainsKey(kvp.Key))
            {
                // Store ONLY the training points
                _charDataService.SkillTrainingLevels[kvp.Key] = kvp.Value;
            }
        }

        string playerName = string.IsNullOrEmpty(_charDataService.PlayerName)
            ? "UnknownPlayer"
            : _charDataService.PlayerName;

        string characterName = string.IsNullOrEmpty(_charDataService.CharacterName)
            ? "UnknownCharacter"
            : _charDataService.CharacterName;

        string fileName = await _persistenceService.GenerateFileName(playerName, characterName);

        bool fileExists = await _persistenceService.CharacterExistsAsync(fileName);
        if (fileExists)
        {
            bool overrideFile = await Application.Current.MainPage.DisplayAlertAsync(
                "File Exists",
                $"A character save named '{fileName}' already exists. Override it?",
                "Yes, Override",
                "No, Cancel");

            if (!overrideFile) return;
        }

        var saveData = _charDataService.CreateSaveData();
        saveData.FileName = fileName;
        saveData.LastSaved = DateTime.Now;
        saveData.CurrentPage = "TrainingPopup";

        await _persistenceService.SaveCharacterDataAsync(saveData);
        _charDataService.MarkCharacterSaved();
        _charDataService.SaveFileName = fileName;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}