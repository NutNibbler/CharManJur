using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class LoadCharacterViewModel : INotifyPropertyChanged
{
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;

    private ObservableCollection<CharacterSaveData> _allCharacters = new();
    private ObservableCollection<CharacterSaveData> _filteredCharacters = new();
    private CharacterSaveData? _selectedCharacter;
    private string _currentFilter = "All";

    public ObservableCollection<CharacterSaveData> FilteredCharacters
    {
        get => _filteredCharacters;
        set
        {
            _filteredCharacters = value;
            OnPropertyChanged();
        }
    }

    public CharacterSaveData? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            _selectedCharacter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedCharacter));
        }
    }

    public bool HasSelectedCharacter => SelectedCharacter != null;

    public bool IsAllFilter => _currentFilter == "All";
    public bool IsIncompleteFilter => _currentFilter == "Incomplete";
    public bool IsCompleteFilter => _currentFilter == "Complete";

    public ICommand LoadCharactersCommand { get; }
    public ICommand SetAllFilterCommand { get; }
    public ICommand SetIncompleteFilterCommand { get; }
    public ICommand SetCompleteFilterCommand { get; }
    public ICommand LoadSelectedCommand { get; }
    public ICommand DeleteSelectedCommand { get; }

    public LoadCharacterViewModel(
        ICharacterPersistenceService persistenceService,
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService)
    {
        _persistenceService = persistenceService;
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;

        LoadCharactersCommand = new Command(async () => await LoadCharactersAsync());
        SetAllFilterCommand = new Command(() => SetFilter("All"));
        SetIncompleteFilterCommand = new Command(() => SetFilter("Incomplete"));
        SetCompleteFilterCommand = new Command(() => SetFilter("Complete"));
        LoadSelectedCommand = new Command(async () => await LoadSelectedAsync());
        DeleteSelectedCommand = new Command(async () => await DeleteSelectedAsync());

        Task.Run(LoadCharactersAsync);
    }

    private async Task LoadCharactersAsync()
    {
        var characters = await _persistenceService.GetAllSavedCharactersAsync();
        _allCharacters = new ObservableCollection<CharacterSaveData>(characters);
        ApplyFilter();
    }

    private void SetFilter(string filter)
    {
        _currentFilter = filter;
        OnPropertyChanged(nameof(IsAllFilter));
        OnPropertyChanged(nameof(IsIncompleteFilter));
        OnPropertyChanged(nameof(IsCompleteFilter));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _currentFilter switch
        {
            "Incomplete" => _allCharacters.Where(c => !c.IsComplete),
            "Complete" => _allCharacters.Where(c => c.IsComplete),
            _ => _allCharacters
        };

        FilteredCharacters = new ObservableCollection<CharacterSaveData>(filtered);
    }

    private async Task LoadSelectedAsync()
    {
        if (SelectedCharacter == null) return;

        var saveData = await _persistenceService.LoadCharacterAsync(SelectedCharacter.FileName);
        if (saveData != null)
        {
            _charDataService.PopulateFromSaveData(saveData);
            _globalMenuDataService.CharBuilderResetRequest();
            _globalMenuDataService.SetCharacterCreationMode(true);

            if (saveData.IsComplete)
            {
                // ===== NAVIGATE TO LIVE GAME CHARACTER HOME PAGE =====
                await Shell.Current.GoToAsync("///CharacterHomePage");
            }
            else
            {
                // Determine where to resume (incomplete character)
                string resumePage = "///CreateNewCharacter";

                if (!string.IsNullOrEmpty(_charDataService.CharacterName) &&
                    !string.IsNullOrEmpty(_charDataService.PlayerName) &&
                    !string.IsNullOrEmpty(_charDataService.CampaignType))
                {
                    resumePage = "///CharacterBuilderHome";
                }

                if (!string.IsNullOrEmpty(_charDataService.CharacterRace))
                {
                    resumePage = "///CharBuilder_Godrick_KinSelection";
                }

                if (!string.IsNullOrEmpty(_charDataService.CharacterClassName))
                {
                    resumePage = "///CharBuilder_Godrick_ClassSelection";
                }

                await Shell.Current.GoToAsync(resumePage);
            }
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedCharacter == null) return;

        bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Delete Character",
            $"Are you sure you want to delete '{SelectedCharacter.CharacterName}'?",
            "Yes",
            "No");

        if (confirm)
        {
            await _persistenceService.DeleteCharacterAsync(SelectedCharacter.FileName);
            await LoadCharactersAsync();
        }
    }

    public async Task RefreshCharactersAsync()
    {
        await LoadCharactersAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}