using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class ApplyDamagePopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onApplied;
    private static readonly int[] DieSizes = { 4, 6, 8, 10, 12, 20 };
    private int? _lastParryRollResult;

    public ApplyDamagePopup(ICharAttribDataService charDataService, Action onApplied)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onApplied = onApplied;

        TypePicker.SelectedIndex = 0;
        ReactionPicker.SelectedIndex = 0;
        ParryDiePicker.SelectedIndex = 0;
    }

    private void OnTypeChanged(object sender, EventArgs e)
    {
        bool isHitpoints = TypePicker.SelectedIndex == (int)DamageTargetType.Hitpoints;
        ReactionSection.IsVisible = isHitpoints;
        NoteLabel.IsVisible = isHitpoints;

        if (!isHitpoints)
        {
            ReactionPicker.SelectedIndex = 0;
            ParrySection.IsVisible = false;
            AmountLabel.Text = "Amount";
        }
    }

    private void OnReactionChanged(object sender, EventArgs e)
    {
        if (ReactionPicker.SelectedIndex < 0) return;
        var reaction = (CombatReactionType)ReactionPicker.SelectedIndex;

        ReactionDescriptionLabel.Text = reaction switch
        {
            CombatReactionType.None => "You take all incoming damage as normal.",
            CombatReactionType.Riposte => "Choose one attacker and deal your damage against it only after you've taken the incoming damage.",
            CombatReactionType.Block => "Incoming Fast or Balanced attacks become Impaired. Blocking with a balanced or heavy weapon, or a shield, fully Negates damage.",
            CombatReactionType.Dodge => "Incoming Slow or Balanced attacks become Impaired. While not wearing heavy armor, incoming Slow or Balanced attacks are fully Negated.",
            CombatReactionType.Parry => "Choose one attacking opponent you can attack in return. Whoever rolls higher deals their damage straight to the other's Vigor, bypassing Hit Protection and any Vigor Save. Equal rolls mean no damage to either side.",
            _ => string.Empty
        };

        ReactionNoteLabel.Text = reaction switch
        {
            CombatReactionType.Riposte => "+1 Fatigue. You may counter-attack after this resolves — not tracked by the app.",
            CombatReactionType.Block => "+1 Fatigue. DM must determine Negate/Impair and roll 1d4 for Impaired damage — enter the resulting Amount below (0 if Negated).",
            CombatReactionType.Dodge => "+1 Fatigue. DM must determine Negate/Impair and roll 1d4 for Impaired damage — enter the resulting Amount below (0 if Negated).",
            _ => string.Empty
        };
        ReactionNoteLabel.IsVisible = !string.IsNullOrEmpty(ReactionNoteLabel.Text);

        ParrySection.IsVisible = reaction == CombatReactionType.Parry;
        AmountLabel.Text = reaction == CombatReactionType.Parry ? "Attacker's Damage Roll" : "Amount";
    }

    private void OnParryRollModeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;
        ParryManualSection.IsVisible = ParryManualRadio.IsChecked;
        ParryRollForMeSection.IsVisible = ParryRollForMeRadio.IsChecked;
    }

    private void OnParryRollClicked(object sender, EventArgs e)
    {
        if (ParryDiePicker.SelectedIndex < 0) return;
        int dieSize = DieSizes[ParryDiePicker.SelectedIndex];
        _lastParryRollResult = Random.Shared.Next(1, dieSize + 1);
        ParryRollResultLabel.Text = $"Rolled: {_lastParryRollResult}";
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        if (TypePicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Missing Type", "Choose a damage type.", "OK");
            return;
        }

        var type = (DamageTargetType)TypePicker.SelectedIndex;
        var reaction = type == DamageTargetType.Hitpoints ? (CombatReactionType)ReactionPicker.SelectedIndex : CombatReactionType.None;

        if (reaction == CombatReactionType.Parry)
        {
            if (!int.TryParse(AmountEntry.Text, out int attackerRoll) || attackerRoll < 0)
            {
                await DisplayAlertAsync("Invalid Amount", "Enter the attacker's damage roll (0 or greater).", "OK");
                return;
            }

            int? parryRoll = ParryRollForMeRadio.IsChecked
                ? _lastParryRollResult
                : (int.TryParse(ParryManualEntry.Text, out int manualRoll) ? manualRoll : (int?)null);

            if (parryRoll == null)
            {
                await DisplayAlertAsync("Missing Roll", "Enter your Parry roll, or use Roll for Me.", "OK");
                return;
            }

            _charDataService.LogCombatReaction(CombatReactionType.Parry, $"Parry: your roll {parryRoll} vs attacker's {attackerRoll}");

            string resultMessage;
            if (parryRoll > attackerRoll)
            {
                resultMessage = $"Parry succeeded! Your {parryRoll} beats their {attackerRoll} — no damage to you. Deal {parryRoll} damage directly to the attacker's Vigor (no save).";
            }
            else if (attackerRoll > parryRoll)
            {
                _charDataService.ApplyParryLoss(attackerRoll);
                resultMessage = $"Parry lost. Their {attackerRoll} beats your {parryRoll} — {attackerRoll} damage dealt directly to your Vigor (no save, no armor).";
            }
            else
            {
                resultMessage = "Parry tied! Both weapons bounce off — no damage to either side.";
            }

            await DisplayAlertAsync("Parry Result", resultMessage, "OK");
            _onApplied?.Invoke();
            await Navigation.PopModalAsync();
            return;
        }

        if (!int.TryParse(AmountEntry.Text, out int amount) || amount < 0)
        {
            await DisplayAlertAsync("Invalid Amount", "Enter a whole number (0 or greater).", "OK");
            return;
        }

        if (reaction != CombatReactionType.None)
        {
            string reactionNote = reaction switch
            {
                CombatReactionType.Riposte => "Used Riposte — will counter-attack after this resolves.",
                CombatReactionType.Block => amount == 0 ? "Used Block — attack fully Negated." : $"Used Block — attack Impaired, {amount} damage entered.",
                CombatReactionType.Dodge => amount == 0 ? "Used Dodge — attack fully Negated." : $"Used Dodge — attack Impaired, {amount} damage entered.",
                _ => $"Used {reaction}."
            };
            _charDataService.LogCombatReaction(reaction, reactionNote);

            if (reaction is CombatReactionType.Riposte or CombatReactionType.Block or CombatReactionType.Dodge)
            {
                _charDataService.AddFatigue(1);
            }
        }

        string message;
        bool needsStabilitySave = false;
        var shifter = LiveShifter.Create(_charDataService);

        if (amount == 0)
        {
            message = "No damage applied.";
        }
        else if (type == DamageTargetType.Hitpoints && shifter.IsTransformed)
        {
            // Transformed: Hit Protection damage hits Stability instead. Direct ability damage (below) is unchanged.
            var outcome = await shifter.TakeDamageAsync(amount);
            message = outcome.Message;
            needsStabilitySave = outcome.NeedsSave;
        }
        else if (type == DamageTargetType.Hitpoints)
        {
            var result = _charDataService.ApplyDamage(amount);
            string armorNote = result.ArmorMitigated > 0 ? $"{result.ArmorMitigated} absorbed by Armor. " : "";
            message = result.VigorLost > 0
                ? $"{armorNote}{result.HitProtectionLost} Hit Protection lost, {result.VigorLost} overflowed to Vigor."
                : $"{armorNote}{result.HitProtectionLost} Hit Protection lost.";
            if (result.IsDead) message += " Vigor reached 0.";
            if (result.TriggersVigorSave) message += " A Vigor Save is now required — check the dashboard.";
        }
        else
        {
            _charDataService.AdjustCurrentStat(type.ToString(), -amount);
            message = $"{type} reduced by {amount}.";
        }

        await DisplayAlertAsync("Damage Applied", message, "OK");

        if (needsStabilitySave)
        {
            // The save page closes itself, then closes this page too.
            await Navigation.PushModalAsync(new StabilitySavePage(shifter, async () =>
            {
                _onApplied?.Invoke();
                await Navigation.PopModalAsync();
            }));
            return;
        }

        _onApplied?.Invoke();
        await Navigation.PopModalAsync();
    }


    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}