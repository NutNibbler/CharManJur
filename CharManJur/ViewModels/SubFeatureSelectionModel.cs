using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// Wrapper class for selectable items with PropertyChanged support
public class SelectableItem<T> : INotifyPropertyChanged
{
    private bool _isSelected;
    private T _item = default!;
    private string _displayName = string.Empty;

    public T Item
    {
        get => _item;
        set
        {
            _item = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                // Notify the parent ViewModel to re-evaluate HasAnySelected
                OnSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            _displayName = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler? OnSelectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SubFeatureSelectionViewModel : INotifyPropertyChanged
{
    private readonly IBlueprintDataService _blueprintDataService;
    private readonly IQuipDataService _quipDataService;
    private readonly ISpellDataService _spellDataService;
    private readonly ITechniqueDataService _techniqueDataService;
    private readonly ICharAttribDataService _charDataService;

    private SubFeatureType _subFeatureType;
    private int _selectedClassId;
    private int _selectedFeatureId;
    private ObservableCollection<SelectableItem<object>> _availableItems = new();
    private bool _isLoading;

    public event EventHandler? SubFeaturesConfirmed;

    public ObservableCollection<SelectableItem<object>> AvailableItems
    {
        get => _availableItems;
        set
        {
            _availableItems = value;
            OnPropertyChanged();
            UpdateHasAnySelected();
        }
    }

    private bool _hasAnySelected;
    public bool HasAnySelected
    {
        get => _hasAnySelected;
        private set
        {
            if (_hasAnySelected != value)
            {
                _hasAnySelected = value;
                OnPropertyChanged();
                // Force the command to re-evaluate
                (ConfirmSelectionCommand as Command)?.ChangeCanExecute();
            }
        }
    }

    private void UpdateHasAnySelected()
    {
        HasAnySelected = AvailableItems.Any(x => x.IsSelected);
    }

    private void OnItemSelectionChanged(object? sender, EventArgs e)
    {
        UpdateHasAnySelected();
    }

    public string SubFeatureTypeName => _subFeatureType.ToString();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadItemsCommand { get; }
    public ICommand ConfirmSelectionCommand { get; }
    public ICommand CancelCommand { get; }

    public SubFeatureSelectionViewModel(
        IBlueprintDataService blueprintDataService,
        IQuipDataService quipDataService,
        ISpellDataService spellDataService,
        ITechniqueDataService techniqueDataService,
        ICharAttribDataService charDataService)
    {
        _blueprintDataService = blueprintDataService;
        _quipDataService = quipDataService;
        _spellDataService = spellDataService;
        _techniqueDataService = techniqueDataService;
        _charDataService = charDataService;

        LoadItemsCommand = new Command(async () => await LoadItemsAsync());
        ConfirmSelectionCommand = new Command(OnConfirmSelection, () => HasAnySelected);
        CancelCommand = new Command(OnCancel);
    }

    public SubFeatureType SubFeatureType => _subFeatureType;

    public void Initialize(SubFeatureType type, int classId, int featureId)
    {
        _subFeatureType = type;
        _selectedClassId = classId;
        _selectedFeatureId = featureId;

        OnPropertyChanged(nameof(SubFeatureTypeName));

        Task.Run(LoadItemsAsync);
    }

    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        try
        {
            List<object> items = new();

            switch (_subFeatureType)
            {
                case SubFeatureType.Blueprint:
                    var blueprints = await _blueprintDataService.GetBlueprintsForCharacterAsync(_selectedClassId, _selectedFeatureId);
                    items.AddRange(blueprints);
                    break;
                case SubFeatureType.Quip:
                    var quips = await _quipDataService.GetQuipsForCharacterAsync(_selectedClassId, _selectedFeatureId);
                    items.AddRange(quips);
                    break;
                case SubFeatureType.Spell:
                    var spells = await _spellDataService.GetSpellsForCharacterAsync(_selectedClassId, _selectedFeatureId);
                    items.AddRange(spells);
                    break;
                case SubFeatureType.Technique:
                    var techniques = await _techniqueDataService.GetTechniquesForCharacterAsync(_selectedClassId, _selectedFeatureId);
                    items.AddRange(techniques);
                    break;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var selectableItems = new List<SelectableItem<object>>();

                foreach (var item in items)
                {
                    var selectable = new SelectableItem<object>
                    {
                        Item = item,
                        IsSelected = false,
                        DisplayName = GetDisplayName(item)
                    };
                    // Subscribe to selection changed event
                    selectable.OnSelectionChanged += OnItemSelectionChanged;
                    selectableItems.Add(selectable);
                }

                AvailableItems = new ObservableCollection<SelectableItem<object>>(selectableItems);
                OnPropertyChanged(nameof(AvailableItems));
                OnPropertyChanged(nameof(SubFeatureTypeName));
                UpdateHasAnySelected();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR: {ex.Message} ===");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetDisplayName(object item)
    {
        return item switch
        {
            Blueprint b => b.BlueprintName,
            Quip q => q.QuipName,
            Spell s => s.SpellName,
            Technique t => t.TechniqueName,
            _ => "Unknown"
        };
    }

    private void OnConfirmSelection()
    {
        var selectedItems = AvailableItems.Where(x => x.IsSelected).Select(x => x.Item).ToList();
        if (!selectedItems.Any()) return;

        foreach (var item in selectedItems)
        {
            switch (_subFeatureType)
            {
                case SubFeatureType.Blueprint:
                    var blueprint = item as Blueprint;
                    if (blueprint != null) _charDataService.AddBlueprint(blueprint);
                    break;
                case SubFeatureType.Quip:
                    var quip = item as Quip;
                    if (quip != null) _charDataService.AddQuip(quip);
                    break;
                case SubFeatureType.Spell:
                    var spell = item as Spell;
                    if (spell != null) _charDataService.AddSpell(spell);
                    break;
                case SubFeatureType.Technique:
                    var technique = item as Technique;
                    if (technique != null) _charDataService.AddTechnique(technique);
                    break;
            }
        }

        SubFeaturesConfirmed?.Invoke(this, EventArgs.Empty);
        Shell.Current.GoToAsync("..");
    }

    private void OnCancel()
    {
        Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}