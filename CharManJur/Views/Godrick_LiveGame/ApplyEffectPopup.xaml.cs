using System.Linq;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

// One form, four ways to use it: apply a new effect, edit one, take on an effect the GM sent (locked),
// or (for the GM) compose an effect to send.
public partial class ApplyEffectPopup : ContentPage
{
    private readonly ICharAttribDataService? _charDataService;
    private readonly Action? _onApplied;
    private readonly CharacterEffect? _editingEffect;
    private readonly IncomingEffect? _incoming;
    private readonly Action<CharacterEffect>? _onComposed;

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
        (EffectTargetType.FatigueCurrent, "Fatigue (Current) - Buff removes, Debuff adds"),
        (EffectTargetType.FatigueMax, "Fatigue (Max) - bar size"),
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
        (EffectTargetType.FatigueCurrent, "Fatigue (Current) - Buff removes, Debuff adds"),
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

        PersistentRadio.IsChecked = true;
        OneShotRadio.IsEnabled = false;
        RefreshTargetList();
        FillFrom(editingEffect);
    }

    // An effect the GM sent: shown locked. The player takes it on or ignores it.
    public ApplyEffectPopup(ICharAttribDataService charDataService, Action onApplied, IncomingEffect incoming)
        : this(charDataService, onApplied)
    {
        _incoming = incoming;
        var effect = incoming.Effect;

        HeaderLabel.Text = "📨 Incoming Effect";
        IncomingBanner.IsVisible = true;
        ApplyButton.Text = "Take On Effect";

        if (effect.Duration == EffectDurationType.OneShot) OneShotRadio.IsChecked = true;   // Persistent is the default
        RefreshTargetList();
        FillFrom(effect);

        // Locked without dimming: taps do nothing and the text boxes can't be typed in, but the text stays easy to read.
        FormFields.InputTransparent = true;
        NameEntry.IsReadOnly = true;
        AmountEntry.IsReadOnly = true;
        NoteEditor.IsReadOnly = true;
        IgnoreHost.IsVisible = true;
        IgnoreButton.IsVisible = true;
    }

    // The GM composing an effect to send: the same form, but it hands the effect back instead of applying it.
    public ApplyEffectPopup(Action<CharacterEffect> onComposed)
    {
        InitializeComponent();
        _onComposed = onComposed;

        HeaderLabel.Text = "📨 Send Effect";
        ApplyButton.Text = "Send to Player";

        RefreshTargetList();
        SkillPicker.SelectedIndex = 0;
    }

    // Puts an effect's name, note and first modifier into the form.
    private void FillFrom(CharacterEffect effect)
    {
        NameEntry.Text = effect.Name;
        NoteEditor.Text = effect.Note;

        if (effect.Modifiers.Count == 0) return;
        var mod = effect.Modifiers[0];

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

        string name = NameEntry.Text.Trim();
        string? note = string.IsNullOrWhiteSpace(NoteEditor.Text) ? null : NoteEditor.Text.Trim();

        // The GM sending it: hand the effect back, nothing is applied here.
        if (_onComposed != null)
        {
            _onComposed(new CharacterEffect
            {
                Name = name,
                Note = note,
                Duration = OneShotRadio.IsChecked ? EffectDurationType.OneShot : EffectDurationType.Persistent,
                Modifiers = modifiers
            });
            await Navigation.PopModalAsync();
            return;
        }

        if (_editingEffect != null)
        {
            // Sent as a copy: EditEffect compares the stored effect with these values to log what changed.
            var updated = new CharacterEffect
            {
                Id = _editingEffect.Id,
                Name = name,
                Note = note,
                Duration = _editingEffect.Duration,
                Modifiers = modifiers
            };
            _charDataService!.EditEffect(updated);
        }
        else
        {
            var effect = new CharacterEffect
            {
                Name = name,
                Note = note,
                Duration = OneShotRadio.IsChecked ? EffectDurationType.OneShot : EffectDurationType.Persistent,
                Modifiers = modifiers
            };
            if (_incoming != null) effect.Id = _incoming.Effect.Id;

            _charDataService!.AddEffect(effect);
            if (_incoming != null) _charDataService.RemoveIncomingEffect(_incoming.Id, ignored: false);
        }

        _onApplied?.Invoke();
        await Navigation.PopModalAsync();
    }

    // The GM sent this by mistake (or the player is skipping it): it is dropped and logged as rule skating.
    private async void OnIgnoreClicked(object sender, EventArgs e)
    {
        if (_incoming == null || _charDataService == null) return;

        bool ok = await DisplayAlertAsync(
            "Ignore Effect",
            "Ignore this effect? Use this only if the GM sent it by mistake. It is logged as rule skating.",
            "Ignore", "Cancel");
        if (!ok) return;

        _charDataService.RemoveIncomingEffect(_incoming.Id, ignored: true);
        _onApplied?.Invoke();
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();   // a sent effect stays waiting
    }
}