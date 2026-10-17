using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// One panel for Spells, Quips, Blueprints and Techniques: the character's own list, with Use and Forget.
public class SubFeaturePanelViewModel : INotifyPropertyChanged
{
    private readonly LiveSubFeatures _live;
    private SubFeatureType _type = SubFeatureType.Spell;
    private SubFeatureEntry? _selected;
    private string _resultText = string.Empty;
    private bool _rebuilding;
    private bool _busy;

    public ObservableCollection<SubFeatureEntry> Rows { get; } = new();

    public ICommand BestowCommand { get; }
    public ICommand ForgetCommand { get; }
    public ICommand UseCommand { get; }
    public ICommand ShowSpellsCommand { get; }
    public ICommand ShowTechniquesCommand { get; }
    public ICommand SandboxCraftCommand { get; }

    // The home page opens the bestow page when this fires.
    public event Action<SubFeatureType>? BestowRequested;
    // Raised when the Spells | Techniques switch changes the type (not when the home page sets it).
    public event Action<SubFeatureType>? TypeChanged;
    // Spells and Techniques open the roll screen; the home page builds it.
    public event Action<SubFeatureEntry>? CastRequested;
    // Blueprints open the craft screen; a null blueprint means sandbox crafting. The home page builds it.
    public event Action<SubFeatureEntry?>? CraftRequested;

    public SubFeaturePanelViewModel(LiveSubFeatures live)
    {
        _live = live;
        BestowCommand = new Command(() => BestowRequested?.Invoke(_type));
        ForgetCommand = new Command(async () => await ForgetAsync());
        UseCommand = new Command(async () => await UseAsync());
        ShowSpellsCommand = new Command(() => Switch(SubFeatureType.Spell));
        ShowTechniquesCommand = new Command(() => Switch(SubFeatureType.Technique));
        SandboxCraftCommand = new Command(() => CraftRequested?.Invoke(null));
    }

    // ===== TYPE =====
    public void SetType(SubFeatureType type)
    {
        _type = type;
        _selected = null;
        _resultText = string.Empty;
        Rebuild();
    }

    private void Switch(SubFeatureType type)
    {
        if (_type == type) return;
        SetType(type);
        TypeChanged?.Invoke(type);
    }

    public void Refresh() => Rebuild();

    private void Rebuild()
    {
        var keep = _selected;

        _rebuilding = true;
        try
        {
            Rows.Clear();
            foreach (var entry in _live.Owned(_type)) Rows.Add(entry);
            _selected = keep == null ? null : Rows.FirstOrDefault(r => r.IsSameAs(keep));
        }
        finally { _rebuilding = false; }

        OnPropertyChanged(string.Empty);
    }

    public SubFeatureEntry? SelectedRow
    {
        get => _selected;
        set
        {
            if (_rebuilding || ReferenceEquals(_selected, value)) return;
            _selected = value;
            _resultText = string.Empty;
            OnPropertyChanged(string.Empty);
        }
    }

    // ===== USE / FORGET =====
    private async Task UseAsync()
    {
        if (_busy || _selected == null) return;

        // Spells and Techniques have their own roll screen.
        if (_selected.Type is SubFeatureType.Spell or SubFeatureType.Technique)
        {
            CastRequested?.Invoke(_selected);
            return;
        }

        // Blueprints open the craft screen.
        if (_selected.Type == SubFeatureType.Blueprint)
        {
            CraftRequested?.Invoke(_selected);
            return;
        }

        _busy = true;
        try
        {
            var result = await _live.UseAsync(_selected);
            _resultText = result.Success ? $"Used {_selected.Name}. Logged for the GM." : result.Message;
            OnPropertyChanged(nameof(ResultText));
            OnPropertyChanged(nameof(HasResult));
        }
        finally { _busy = false; }
    }

    private async Task ForgetAsync()
    {
        if (_busy || _selected == null) return;
        var page = Application.Current!.MainPage!;
        var entry = _selected;

        bool confirmed = await page.DisplayAlertAsync(
            $"Forget {LiveSubFeatures.TypeName(entry.Type)}",
            $"Forget {entry.Name}? This is permanent. It only comes back if it is bestowed again.",
            "Forget", "Cancel");
        if (!confirmed) return;

        _busy = true;
        try
        {
            var result = await _live.ForgetAsync(entry);
            if (!result.Success)
            {
                await page.DisplayAlertAsync("Couldn't Forget", result.Message, "OK");
                return;
            }

            _selected = null;
            _resultText = $"Forgot {entry.Name}.";
            Rebuild();
        }
        finally { _busy = false; }
    }

    // ===== DISPLAY =====
    public string Title => _type == SubFeatureType.Blueprint
        ? "🔨 Crafting"
        : $"{LiveSubFeatures.Icon(_type)} {LiveSubFeatures.PluralName(_type)}";
    public string TypeName => LiveSubFeatures.TypeName(_type);
    public string BestowText => $"➕ Bestow {TypeName}";
    public string ListHeader => _type == SubFeatureType.Blueprint
        ? $"Blueprints you know ({Rows.Count})"
        : $"{LiveSubFeatures.PluralName(_type)} you know ({Rows.Count})";
    public string EmptyText => _type == SubFeatureType.Blueprint
        ? "No blueprints yet. You can still craft simple things without one."
        : $"No {LiveSubFeatures.PluralName(_type).ToLowerInvariant()} yet. Use Bestow to add one.";

    public bool ShowTypeSwitch => _type is SubFeatureType.Spell or SubFeatureType.Technique;
    public bool IsSpellView => _type == SubFeatureType.Spell;
    public bool IsTechniqueView => _type == SubFeatureType.Technique;

    public bool HasNoRows => Rows.Count == 0;
    public bool HasSelection => _selected != null;
    public bool HasNoSelection => _selected == null;

    public string SelectedName => _selected?.Name ?? string.Empty;
    public string SelectedDescription => _selected?.Description ?? string.Empty;
    public bool HasSelectedDescription => _selected?.HasDescription ?? false;
    public string SelectedDetail => _selected?.DetailLine ?? string.Empty;
    public bool HasSelectedDetail => _selected?.HasDetailLine ?? false;

    public bool CanUse => _selected?.Type is SubFeatureType.Quip or SubFeatureType.Spell
    or SubFeatureType.Technique or SubFeatureType.Blueprint;
    public string UseText => _selected?.Type switch
    {
        SubFeatureType.Spell => "🎲 Cast",
        SubFeatureType.Technique => "🌀 Sculpt",
        SubFeatureType.Blueprint => "🔨 Craft With This",
        _ => "⚡ Use"
    };

    // Sandbox crafting (no blueprint) lives in the Blueprints panel.
    public bool CanSandboxCraft => _type == SubFeatureType.Blueprint;
    public bool IsCraftingView => _type == SubFeatureType.Blueprint;
    public string CraftingHint => "Blueprints are guidelines: they let you craft more complex things. " +
                                  "Simple things can be crafted without one (your GM may ask for an Artifice roll).";

    // Shown under the details after a cast finishes (the roll screen reports back here).
    public void ShowResult(string text)
    {
        _resultText = text;
        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(HasResult));
    }

    public string ResultText => _resultText;
    public bool HasResult => _resultText.Length > 0;

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}