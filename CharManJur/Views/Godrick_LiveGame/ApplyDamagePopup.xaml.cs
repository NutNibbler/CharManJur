using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class ApplyDamagePopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onApplied;
    private readonly IncomingDamage? _incoming;   // set when the GM sent this damage: its details are locked
    private static readonly int[] DieSizes = { 4, 6, 8, 10, 12, 20 };
    private int? _lastParryRollResult;

    public ApplyDamagePopup(ICharAttribDataService charDataService, Action onApplied, IncomingDamage? incoming = null)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onApplied = onApplied;
        _incoming = incoming;

        TypePicker.SelectedIndex = 0;
        ReactionPicker.SelectedIndex = 0;
        ParryDiePicker.SelectedIndex = 0;

        if (incoming != null) ShowIncoming(incoming);
    }

    // Damage the GM sent: the type, amount and speed are shown locked, and the player only picks a reaction.
    private void ShowIncoming(IncomingDamage incoming)
    {
        HeaderLabel.Text = "📨 Incoming Damage";
        IncomingSection.IsVisible = true;
        IncomingSummaryLabel.Text = incoming.Summary;

        if (!string.IsNullOrWhiteSpace(incoming.Note))
        {
            IncomingNoteLabel.Text = incoming.Note;
            IncomingNoteLabel.IsVisible = true;
        }

        TypeSection.IsVisible = false;
        AmountSection.IsVisible = false;
        IgnoreHost.IsVisible = true;
        IgnoreButton.IsVisible = true;

        // The picker still holds the type, so the rest of the flow works as it always did.
        TypePicker.SelectedIndex = (int)incoming.Target;
        UpdateIncomingHints((CombatReactionType)ReactionPicker.SelectedIndex);
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
            CombatReactionType.Block or CombatReactionType.Dodge => _incoming != null
                ? "+1 Fatigue. Work out Negate or Impair with your GM, then enter what actually lands below (0 if Negated)."
                : "+1 Fatigue. DM must determine Negate/Impair and roll 1d4 for Impaired damage — enter the resulting Amount below (0 if Negated).",
            _ => string.Empty
        };
        ReactionNoteLabel.IsVisible = !string.IsNullOrEmpty(ReactionNoteLabel.Text);

        ParrySection.IsVisible = reaction == CombatReactionType.Parry;
        AmountLabel.Text = reaction == CombatReactionType.Parry ? "Attacker's Damage Roll" : "Amount";

        UpdateIncomingHints(reaction);
    }

    // For sent damage: say how the attack's speed meets the chosen reaction, and offer the "what lands" field.
    private void UpdateIncomingHints(CombatReactionType reaction)
    {
        if (_incoming == null || _incoming.Target != DamageTargetType.Hitpoints)
        {
            ResultSection.IsVisible = false;
            SpeedHintLabel.IsVisible = false;
            return;
        }

        var speed = _incoming.Speed;
        SpeedHintLabel.Text = reaction switch
        {
            CombatReactionType.Block => speed == IncomingAttackSpeed.Slow
                ? "This is a Slow attack, so Block doesn't Impair it."
                : $"This is a {speed} attack, so Block makes it Impaired.",
            CombatReactionType.Dodge => speed == IncomingAttackSpeed.Fast
                ? "This is a Fast attack, so Dodge doesn't Impair it."
                : $"This is a {speed} attack, so Dodge makes it Impaired.",
            _ => string.Empty
        };
        SpeedHintLabel.IsVisible = !string.IsNullOrEmpty(SpeedHintLabel.Text);

        bool needsResult = reaction is CombatReactionType.Block or CombatReactionType.Dodge;
        ResultSection.IsVisible = needsResult;
        if (needsResult) ResultEntry.Text = _incoming.Amount.ToString();   // starts at the GM's amount; the player adjusts it
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

    // The damage has been dealt with: it no longer waits on the character.
    private void FinishIncoming()
    {
        if (_incoming != null) _charDataService.RemoveIncomingDamage(_incoming.Id, ignored: false);
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
            int attackerRoll;
            if (_incoming != null)
            {
                attackerRoll = _incoming.Amount;   // the GM's amount is the attacker's roll
            }
            else if (!int.TryParse(AmountEntry.Text, out attackerRoll) || attackerRoll < 0)
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

            FinishIncoming();
            await DisplayAlertAsync("Parry Result", resultMessage, "OK");
            _onApplied?.Invoke();
            await Navigation.PopModalAsync();
            return;
        }

        // The amount that lands: typed in, or (for damage the GM sent) the GM's number, or what the player enters after Block or Dodge.
        int amount;
        if (_incoming == null)
        {
            if (!int.TryParse(AmountEntry.Text, out amount) || amount < 0)
            {
                await DisplayAlertAsync("Invalid Amount", "Enter a whole number (0 or greater).", "OK");
                return;
            }
        }
        else if (reaction is CombatReactionType.Block or CombatReactionType.Dodge)
        {
            if (!int.TryParse(ResultEntry.Text, out amount) || amount < 0)
            {
                await DisplayAlertAsync("Invalid Amount", "Enter the damage that lands after your reaction (0 or greater).", "OK");
                return;
            }
        }
        else
        {
            amount = _incoming.Amount;
        }

        if (reaction != CombatReactionType.None)
        {
            string sent = _incoming != null ? $" (the GM sent {_incoming.Amount})" : string.Empty;
            string reactionNote = reaction switch
            {
                CombatReactionType.Riposte => "Used Riposte — will counter-attack after this resolves.",
                CombatReactionType.Block => amount == 0 ? "Used Block — attack fully Negated." : $"Used Block — attack Impaired, {amount} damage lands{sent}.",
                CombatReactionType.Dodge => amount == 0 ? "Used Dodge — attack fully Negated." : $"Used Dodge — attack Impaired, {amount} damage lands{sent}.",
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

        FinishIncoming();
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

    // The GM sent this by mistake (or the player is skipping it): it is dropped and logged as rule skating.
    private async void OnIgnoreClicked(object sender, EventArgs e)
    {
        if (_incoming == null) return;

        bool ok = await DisplayAlertAsync(
            "Ignore Damage",
            "Ignore this damage? Use this only if the GM sent it by mistake. It is logged as rule skating.",
            "Ignore", "Cancel");
        if (!ok) return;

        _charDataService.RemoveIncomingDamage(_incoming.Id, ignored: true);
        _onApplied?.Invoke();
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();   // sent damage stays waiting
    }
}