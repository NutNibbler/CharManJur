using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class CustomHinderanceBuilderViewModel : INotifyPropertyChanged
{
    private readonly IHinderanceDataService _hinderanceDataService;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private int _vigorModifier = 0;
    private int _agilityModifier = 0;
    private int _mindModifier = 0;
    private int _spiritModifier = 0;

    private ObservableCollection<HinderanceSkillModifier> _skillModifiers = new();
    private string _selectedSkillName = string.Empty;
    private int _selectedSkillModifierAmount = -1;

    private bool _isEditMode;
    private Hinderance? _editingHinderance;

    public List<string> PredefinedSkills { get; } = new()
    {
        "Athletics", "Acrobatics", "Aim", "Arcana", "Artifice",
        "Commune", "Constitution", "Deception", "Diplomacy", "Drive",
        "Grapple", "Heal", "Investigate", "Lore", "Sight",
        "Presence", "Ride", "Stealth", "Survival", "Thief"
    };

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public int VigorModifier
    {
        get => _vigorModifier;
        set { _vigorModifier = value; OnPropertyChanged(); }
    }

    public int AgilityModifier
    {
        get => _agilityModifier;
        set { _agilityModifier = value; OnPropertyChanged(); }
    }

    public int MindModifier
    {
        get => _mindModifier;
        set { _mindModifier = value; OnPropertyChanged(); }
    }

    public int SpiritModifier
    {
        get => _spiritModifier;
        set { _spiritModifier = value; OnPropertyChanged(); }
    }

    public ObservableCollection<HinderanceSkillModifier> SkillModifiers
    {
        get => _skillModifiers;
        set { _skillModifiers = value; OnPropertyChanged(); }
    }

    public string SelectedSkillName
    {
        get => _selectedSkillName;
        set { _selectedSkillName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddSkillModifier)); }
    }

    public int SelectedSkillModifierAmount
    {
        get => _selectedSkillModifierAmount;
        set { _selectedSkillModifierAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddSkillModifier)); }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(SaveButtonText));
            }
        }
    }

    public string PageTitle => IsEditMode ? "Edit Custom Hinderance" : "Create Custom Hinderance";
    public string SaveButtonText => IsEditMode ? "Update Hinderance" : "Create Hinderance";

    public bool CanSave => !string.IsNullOrWhiteSpace(Name);
    public bool CanAddSkillModifier => !string.IsNullOrWhiteSpace(SelectedSkillName) && SelectedSkillModifierAmount != 0;

    public ICommand AddSkillModifierCommand { get; }
    public ICommand RemoveSkillModifierCommand { get; }
    public ICommand SaveHinderanceCommand { get; }
    public ICommand CancelCommand { get; }

    public ICommand IncrementVigorCommand { get; }
    public ICommand DecrementVigorCommand { get; }
    public ICommand IncrementAgilityCommand { get; }
    public ICommand DecrementAgilityCommand { get; }
    public ICommand IncrementMindCommand { get; }
    public ICommand DecrementMindCommand { get; }
    public ICommand IncrementSpiritCommand { get; }
    public ICommand DecrementSpiritCommand { get; }
    public ICommand IncrementSkillModifierCommand { get; }
    public ICommand DecrementSkillModifierCommand { get; }

    public CustomHinderanceBuilderViewModel(IHinderanceDataService hinderanceDataService)
    {
        _hinderanceDataService = hinderanceDataService;

        AddSkillModifierCommand = new Command(AddSkillModifier);
        RemoveSkillModifierCommand = new Command<HinderanceSkillModifier>(RemoveSkillModifier);
        SaveHinderanceCommand = new Command(async () => await SaveHinderanceAsync());
        CancelCommand = new Command(async () => await CancelAsync());

        IncrementVigorCommand = new Command(() => ChangeStat(ref _vigorModifier, 1, nameof(VigorModifier)));
        DecrementVigorCommand = new Command(() => ChangeStat(ref _vigorModifier, -1, nameof(VigorModifier)));
        IncrementAgilityCommand = new Command(() => ChangeStat(ref _agilityModifier, 1, nameof(AgilityModifier)));
        DecrementAgilityCommand = new Command(() => ChangeStat(ref _agilityModifier, -1, nameof(AgilityModifier)));
        IncrementMindCommand = new Command(() => ChangeStat(ref _mindModifier, 1, nameof(MindModifier)));
        DecrementMindCommand = new Command(() => ChangeStat(ref _mindModifier, -1, nameof(MindModifier)));
        IncrementSpiritCommand = new Command(() => ChangeStat(ref _spiritModifier, 1, nameof(SpiritModifier)));
        DecrementSpiritCommand = new Command(() => ChangeStat(ref _spiritModifier, -1, nameof(SpiritModifier)));

        IncrementSkillModifierCommand = new Command(() => ChangeSkillModifierAmount(1));
        DecrementSkillModifierCommand = new Command(() => ChangeSkillModifierAmount(-1));
    }

    private void ChangeStat(ref int field, int delta, string propertyName)
    {
        int newValue = field + delta;
        if (newValue < -10) newValue = -10;
        if (newValue > 10) newValue = 10;
        field = newValue;
        OnPropertyChanged(propertyName);
    }

    private void ChangeSkillModifierAmount(int delta)
    {
        int newValue = SelectedSkillModifierAmount + delta;
        if (newValue < -5) newValue = -5;
        if (newValue > 5) newValue = 5;
        SelectedSkillModifierAmount = newValue;
    }

    private void AddSkillModifier()
    {
        if (!CanAddSkillModifier) return;

        var existing = SkillModifiers.FirstOrDefault(s => s.SkillName == SelectedSkillName);
        if (existing != null)
        {
            existing.Modifier = SelectedSkillModifierAmount;
        }
        else
        {
            SkillModifiers.Add(new HinderanceSkillModifier
            {
                SkillName = SelectedSkillName,
                Modifier = SelectedSkillModifierAmount
            });
        }

        SelectedSkillName = string.Empty;
        SelectedSkillModifierAmount = -1;
    }

    private void RemoveSkillModifier(HinderanceSkillModifier? modifier)
    {
        if (modifier == null) return;
        SkillModifiers.Remove(modifier);
    }

    public void LoadHinderanceForEdit(Hinderance hinderance)
    {
        _editingHinderance = hinderance;
        IsEditMode = true;

        Name = hinderance.Name;
        Description = hinderance.Description;
        VigorModifier = hinderance.VigorModifier;
        AgilityModifier = hinderance.AgilityModifier;
        MindModifier = hinderance.MindModifier;
        SpiritModifier = hinderance.SpiritModifier;

        SkillModifiers.Clear();
        foreach (var modifier in hinderance.SkillModifiers)
        {
            SkillModifiers.Add(new HinderanceSkillModifier { SkillName = modifier.SkillName, Modifier = modifier.Modifier });
        }

        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(VigorModifier));
        OnPropertyChanged(nameof(AgilityModifier));
        OnPropertyChanged(nameof(MindModifier));
        OnPropertyChanged(nameof(SpiritModifier));
        OnPropertyChanged(nameof(SkillModifiers));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CanSave));
    }

    private async Task SaveHinderanceAsync()
    {
        if (!CanSave) return;

        try
        {
            if (IsEditMode && _editingHinderance != null)
            {
                _editingHinderance.Name = Name;
                _editingHinderance.Description = Description;
                _editingHinderance.VigorModifier = VigorModifier;
                _editingHinderance.AgilityModifier = AgilityModifier;
                _editingHinderance.MindModifier = MindModifier;
                _editingHinderance.SpiritModifier = SpiritModifier;
                _editingHinderance.SkillModifiers = SkillModifiers.ToList();

                await _hinderanceDataService.UpdateHinderanceAsync(_editingHinderance);
            }
            else
            {
                var request = new CreateCustomHinderanceRequest
                {
                    Name = Name,
                    Description = Description,
                    VigorModifier = VigorModifier,
                    AgilityModifier = AgilityModifier,
                    MindModifier = MindModifier,
                    SpiritModifier = SpiritModifier,
                    SkillModifiers = SkillModifiers.ToList()
                };

                await _hinderanceDataService.CreateCustomHinderanceAsync(request);
            }

            Preferences.Default.Set("RefreshHinderances", true);

            string message = IsEditMode
                ? $"Hinderance '{Name}' has been updated successfully!"
                : $"Hinderance '{Name}' has been created successfully!";

            await Application.Current.MainPage.DisplayAlertAsync("Success", message, "OK");
            await Shell.Current.GoToAsync("//CharBuilder_Godrick_HinderanceSelection");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlertAsync("Error", $"Failed to save hinderance: {ex.Message}", "OK");
        }
    }

    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("//CharBuilder_Godrick_HinderanceSelection");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}