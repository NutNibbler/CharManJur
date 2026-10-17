using System.Linq;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class ApplyEffectPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action _onApplied;
    private readonly CharacterEffect? _editingEffect;

    private static readonly (EffectTargetType Target, string Label)[] PersistentTargets =
    {
        (EffectTargetType.VigorCurrent, "Vigor (Current)"),
        (EffectTargetType.VigorMax, "Vigor (Max)"),
        (EffectTargetType.AgilityCurrent, "Agility (Current)"),
        (EffectTargetType.AgilityMax, "Agility (Max)"),
        (EffectTargetType.MindCurrent, "Mind (Current)"),
        (EffectTargetType.MindMax, "Mind (Max)"),
        (EffectTargetType.SpiritCurrent, "Spirit (Current)"),
        (EffectTargetType.SpiritMax, "Spirit (Max)"),
        (EffectTargetType.HitpointsCurrent, "Hitpoints (Current)"),
        (EffectTargetType.HitpointsMax, "Hitpoints (Max)"),
        (EffectTargetType.Skill, "Skill"),
    };

    // One-Shot effects can only touch a Current value — no Max, no Skill.
    private static readonly (EffectTargetType Target, string Label)[] OneShotTargets =
    {
        (EffectTargetType.VigorCurrent, "Vigor (Current)"),
        (EffectTargetType.AgilityCurrent, "Agility (Current)"),
        (EffectTargetType.MindCurrent, "Mind (Current)"),
        (EffectTargetType.SpiritCurrent, "Spirit (Current)"),
        (EffectTargetType.HitpointsCurrent, "Hitpoints (Current)"),
    };

    private (EffectTargetType Target, string Label)[] _currentTargetSet = PersistentTargets;

    // New effect
    public ApplyEffectPopup(ICharAttribDataService charDataService, Action onApplied)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onApplied = onApplied;
        _editingEffect = null;

        RefreshTargetList();
        SkillPicker.SelectedIndex = 0;
    }

    // Edit existing effect — always Persistent, since One-Shot effects are never stored.
    public ApplyEffectPopup(ICharAttribDataService charDataService, Action onApplied, CharacterEffect editingEffect)
        : this(charDataService, onApplied)
    {
        _editingEffect = editingEffect;

        HeaderLabel.Text = "✏️ Edit Effect";
        ApplyButton.Text = "Save Changes";

        NameEntry.Text = editingEffect.Name;
        NoteEditor.Text = editingEffect.Note;

        PersistentRadio.IsChecked = true;
        OneShotRadio.IsEnabled = false;
        RefreshTargetList();

        if (editingEffect.Modifiers.Count > 0)
        {
            var mod = editingEffect.Modifiers[0];
            int idx = Array.FindIndex(_currentTargetSet, t => t.Target == mod.Target);
            if (idx >= 0) TargetPicker.SelectedIndex = idx;

            if (mod.Target == EffectTargetType.Skill && mod.SkillName != null)
            {
                SkillPicker.SelectedItem = mod.SkillName;
            }

            AmountEntry.Text = Math.Abs(mod.Amount).ToString();
            if (mod.Amount < 0) DebuffRadio.IsChecked = true;
            else BuffRadio.IsChecked = true;
        }
    }

    private void OnDurationChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return;   // fires twice per toggle; only act on the newly-checked one
        RefreshTargetList();
    }

    private void RefreshTargetList()
    {
        _currentTargetSet = OneShotRadio.IsChecked ? OneShotTargets : PersistentTargets;
        TargetPicker.ItemsSource = _currentTargetSet.Select(t => t.Label).ToList();
        TargetPicker.SelectedIndex = 0;
    }

    private void OnTargetChanged(object sender, EventArgs e)
    {
        if (TargetPicker.SelectedIndex < 0) return;
        var target = _currentTargetSet[TargetPicker.SelectedIndex].Target;
        SkillPickerSection.IsVisible = target == EffectTargetType.Skill;
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Name Required", "Give this effect a name.", "OK");
            return;
        }

        if (TargetPicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Missing Target", "Choose what this effect modifies.", "OK");
            return;
        }

        if (!int.TryParse(AmountEntry.Text, out int magnitude) || magnitude <= 0)
        {
            await DisplayAlertAsync("Invalid Amount", "Enter a whole number greater than 0.", "OK");
            return;
        }

        var target = _currentTargetSet[TargetPicker.SelectedIndex].Target;
        int amount = DebuffRadio.IsChecked ? -magnitude : magnitude;
        string? skillName = target == EffectTargetType.Skill ? SkillPicker.SelectedItem as string : null;

        if (target == EffectTargetType.Skill && string.IsNullOrEmpty(skillName))
        {
            await DisplayAlertAsync("Missing Skill", "Choose a skill.", "OK");
            return;
        }

        var modifiers = new List<EffectModifier>
        {
            new EffectModifier { Target = target, SkillName = skillName, Amount = amount }
        };

        if (_editingEffect != null)
        {
            // Sent as a copy: EditEffect compares the stored effect with these values to log what changed.
            var updated = new CharacterEffect
            {
                Id = _editingEffect.Id,
                Name = NameEntry.Text.Trim(),
                Note = string.IsNullOrWhiteSpace(NoteEditor.Text) ? null : NoteEditor.Text.Trim(),
                Duration = _editingEffect.Duration,
                Modifiers = modifiers
            };
            _charDataService.EditEffect(updated);
        }
        else
        {
            var effect = new CharacterEffect
            {
                Name = NameEntry.Text.Trim(),
                Note = string.IsNullOrWhiteSpace(NoteEditor.Text) ? null : NoteEditor.Text.Trim(),
                Duration = OneShotRadio.IsChecked ? EffectDurationType.OneShot : EffectDurationType.Persistent,
                Modifiers = modifiers
            };
            _charDataService.AddEffect(effect);
        }

        _onApplied?.Invoke();
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}