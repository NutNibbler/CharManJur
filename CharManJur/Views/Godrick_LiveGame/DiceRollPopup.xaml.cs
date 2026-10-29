using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class DiceRollPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private List<string> _skillNames = new();
    private int? _checkRollResult;
    private int? _advantageDieResult;
    private int? _saveRollResult;
    private static readonly Dictionary<string, string> SkillAbilities = new()
    {
        ["Athletics"] = "Vigor",
        ["Constitution"] = "Vigor",
        ["Grapple"] = "Vigor",
        ["Presence"] = "Vigor",
        ["Ride"] = "Vigor",
        ["Acrobatics"] = "Agility",
        ["Aim"] = "Agility",
        ["Drive"] = "Agility",
        ["Stealth"] = "Agility",
        ["Thief"] = "Agility",
        ["Arcana"] = "Mind",
        ["Artifice"] = "Mind",
        ["Heal"] = "Mind",
        ["Investigate"] = "Mind",
        ["Lore"] = "Mind",
        ["Commune"] = "Spirit",
        ["Deception"] = "Spirit",
        ["Diplomacy"] = "Spirit",
        ["Sight"] = "Spirit",
        ["Survival"] = "Spirit",
    };

    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();

    private static Color? AbilityColor(string ability)
    {
        string key = ability + "Color";   // VigorColor, AgilityColor, MindColor, SpiritColor
        return Application.Current != null
               && Application.Current.Resources.TryGetValue(key, out var value)
               && value is Color color ? color : null;
    }

    private int GetCurrentAsm(string ability) => ability switch
    {
        "Vigor" => _charDataService.CurrentASMStatVigor,
        "Agility" => _charDataService.CurrentASMStatAgility,
        "Mind" => _charDataService.CurrentASMStatMind,
        "Spirit" => _charDataService.CurrentASMStatSpirit,
        _ => 0
    };

    private static void StyleAbilityLabel(Label label, string ability, int stat, int asm)
    {
        label.Text = $"{ability.ToUpper()}  {stat}   ·   ASM {Signed(asm)}";
        var color = AbilityColor(ability);
        if (color != null) label.TextColor = color;
    }
    public DiceRollPopup(ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _charDataService = charDataService;

        _skillNames = _charDataService.SkillTrainingLevels.Keys.OrderBy(s => s).ToList();
        SkillPicker.ItemsSource = _skillNames;
        if (_skillNames.Count > 0) SkillPicker.SelectedIndex = 0;

        AbilityPicker.SelectedIndex = 0;
        UpdateSaveTarget();
    }

    private void OnModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        CheckSection.IsVisible = CheckRadio.IsChecked;
        SaveSection.IsVisible = SaveRadio.IsChecked;
    }

    private void OnSkillChanged(object sender, EventArgs e)
    {
        if (SkillPicker.SelectedIndex < 0) return;
        string skill = _skillNames[SkillPicker.SelectedIndex];

        var b = _charDataService.GetSkillBreakdown(skill);
        string ability = SkillAbilities.TryGetValue(skill, out var a) ? a : "Vigor";

        StyleAbilityLabel(SkillAbilityLabel, ability, GetCurrentStat(ability), GetCurrentAsm(ability));

        SkillBreakdownLabel.Text =
            $"Base {Signed(b.NaturalNegative)}  ·  ASM {Signed(b.ASM)}  ·  Training {Signed(b.TrainingPoints)}  ·  Other {Signed(b.OtherModifiers)}";

        int modifier = _charDataService.GetSkillTotal(skill);
        SkillFormulaLabel.Text = $"{skill}: 2d6 {Signed(modifier)}";
    }

    private void OnAdvantageModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        AdvantageDieSection.IsVisible = !AdvantageNoneRadio.IsChecked;
    }

    private void OnAbilityChanged(object sender, EventArgs e) => UpdateSaveTarget();

    private void UpdateSaveTarget()
    {
        if (AbilityPicker.SelectedIndex < 0) return;
        string ability = SelectedAbility();
        int target = GetCurrentStat(ability);

        StyleAbilityLabel(SaveAbilityLabel, ability, target, GetCurrentAsm(ability));
        SaveTargetLabel.Text = $"Roll d20 ≤ {target} to succeed (natural 1 always succeeds, natural 20 always fails)";
    }

    private string SelectedAbility() => AbilityPicker.SelectedItem as string ?? "Vigor";

    private int GetCurrentStat(string ability) => ability switch
    {
        "Vigor" => _charDataService.CurrentStatVigor,
        "Agility" => _charDataService.CurrentStatAgility,
        "Mind" => _charDataService.CurrentStatMind,
        "Spirit" => _charDataService.CurrentStatSpirit,
        _ => 0
    };

    private void OnCheckRollModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        CheckManualSection.IsVisible = CheckManualRadio.IsChecked;
        CheckRollForMeSection.IsVisible = CheckRollForMeRadio.IsChecked;
    }

    private void OnCheckRollClicked(object sender, EventArgs e)
    {
        int die1 = Random.Shared.Next(1, 7);
        int die2 = Random.Shared.Next(1, 7);
        _checkRollResult = die1 + die2;
        CheckRollResultLabel.Text = $"Rolled: {die1} + {die2} = {_checkRollResult}";
    }

    private void OnAdvantageRollClicked(object sender, EventArgs e)
    {
        _advantageDieResult = Random.Shared.Next(1, 5);
        AdvantageRollResultLabel.Text = $"Rolled: {_advantageDieResult}";
    }

    private void OnSaveRollModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        SaveManualSection.IsVisible = SaveManualRadio.IsChecked;
        SaveRollForMeSection.IsVisible = SaveRollForMeRadio.IsChecked;
    }

    private void OnSaveRollClicked(object sender, EventArgs e)
    {
        _saveRollResult = Random.Shared.Next(1, 21);
        SaveRollResultLabel.Text = $"Rolled: {_saveRollResult}";
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (CheckRadio.IsChecked)
        {
            if (SkillPicker.SelectedIndex < 0)
            {
                await DisplayAlertAsync("Missing Skill", "Choose a skill.", "OK");
                return;
            }

            int? diceTotal = CheckManualRadio.IsChecked
                ? (int.TryParse(CheckManualEntry.Text, out int manual) ? manual : (int?)null)
                : _checkRollResult;

            if (diceTotal == null)
            {
                await DisplayAlertAsync("Missing Roll", "Enter your 2d6 total, or use Roll for Me.", "OK");
                return;
            }

            string advantageMode = AdvantageNoneRadio.IsChecked ? "None" : AdvantageRadio.IsChecked ? "Advantage" : "Disadvantage";
            int? advantageDie = null;

            if (advantageMode != "None")
            {
                advantageDie = CheckManualRadio.IsChecked
                    ? (int.TryParse(AdvantageManualEntry.Text, out int manualDie) ? manualDie : (int?)null)
                    : _advantageDieResult;

                if (advantageDie == null || advantageDie < 1 || advantageDie > 4)
                {
                    await DisplayAlertAsync("Missing d4 Roll", "Enter or roll a d4 result from 1 to 4.", "OK");
                    return;
                }
            }

            string skill = _skillNames[SkillPicker.SelectedIndex];
            int modifier = _charDataService.GetSkillTotal(skill);
            int adjustment = advantageMode == "Advantage" ? advantageDie ?? 0 : advantageMode == "Disadvantage" ? -(advantageDie ?? 0) : 0;
            int total = diceTotal.Value + modifier + adjustment;

            _charDataService.LogSkillCheck(skill, diceTotal.Value, modifier, advantageMode, advantageDie);

            string breakdown = advantageMode == "None"
                ? $"{skill}: {diceTotal} + {modifier} = {total}"
                : $"{skill}: {diceTotal} {(adjustment >= 0 ? "+" : "-")}{advantageDie} ({advantageMode}) + {modifier} = {total}";

            await DisplayAlertAsync("Check Result", $"{breakdown}\n\nReport this total to your GM.", "OK");
        }
        else
        {
            int? roll = SaveManualRadio.IsChecked
                ? (int.TryParse(SaveManualEntry.Text, out int manual) ? manual : (int?)null)
                : _saveRollResult;

            if (roll == null || roll < 1 || roll > 20)
            {
                await DisplayAlertAsync("Invalid Roll", "Enter or roll a value from 1 to 20.", "OK");
                return;
            }

            string ability = SelectedAbility();
            int target = GetCurrentStat(ability);
            bool success = roll == 1 || (roll != 20 && roll <= target);

            _charDataService.LogAbilitySave(ability, roll.Value, success);

            await DisplayAlertAsync(
                success ? "Save Passed" : "Save Failed",
                $"{ability} Save: rolled {roll} against {target} — {(success ? "Passed" : "Failed")}",
                "OK");
        }

        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}