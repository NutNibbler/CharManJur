using CharManJur.Models;
using CharManJur.Services;
using System.Xml;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class EffectDetailPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly CharacterEffect _effect;
    private readonly Action _onChanged;

    public EffectDetailPopup(CharacterEffect effect, ICharAttribDataService charDataService, Action onChanged)
    {
        InitializeComponent();
        _effect = effect;
        _charDataService = charDataService;
        _onChanged = onChanged;

        NameLabel.Text = effect.Name;
        SummaryLabel.Text = Summarize(effect);
        NoteLabel.Text = string.IsNullOrWhiteSpace(effect.Note) ? "(no note)" : effect.Note;
    }

    private static string Summarize(CharacterEffect effect)
    {
        if (effect.Modifiers.Count == 0) return "No modifiers.";
        var m = effect.Modifiers[0];
        string targetLabel = m.Target == EffectTargetType.Skill ? $"Skill: {m.SkillName}" : m.Target.ToString();
        return $"{targetLabel} {(m.Amount >= 0 ? "+" : "")}{m.Amount}";
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        // The editor opens on top of this page, so this page stays in the navigation stack.
        var editor = new ApplyEffectPopup(_charDataService, _onChanged, _effect);
        await Navigation.PushModalAsync(editor);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Back from the editor: show what was saved.
        NameLabel.Text = _effect.Name;
        SummaryLabel.Text = Summarize(_effect);
        NoteLabel.Text = string.IsNullOrWhiteSpace(_effect.Note) ? "(no note)" : _effect.Note;
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Clear Effect", $"Remove '{_effect.Name}'?", "Yes", "Cancel");
        if (!confirm) return;

        _charDataService.RemoveEffect(_effect.Id);
        _onChanged?.Invoke();
        await Navigation.PopModalAsync();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}