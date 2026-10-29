using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class BioPage : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly BioSheetViewModel _sheet;
    private readonly Func<Task>? _onClosed;
    private bool _storyDirty = false;
    private readonly LevelUpService _levelUp;
    private readonly IItemDataService _items;
    private readonly ProficiencyTrackerViewModel _tracker;

    public BioPage(ICharAttribDataService charDataService, WeaponProficiencyTracker proficiency,
               IItemDataService items, IPlayerActionLogService log, Func<Task>? onClosed)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onClosed = onClosed;

        _sheet = new BioSheetViewModel(charDataService, new TrainingManager(charDataService, log));
        BindingContext = _sheet;

        _items = items;
        _levelUp = LevelUpService.Create(charDataService, IPlatformApplication.Current!.Services);
        _tracker = new ProficiencyTrackerViewModel(proficiency, items);
        ProficiencyTracker.BindingContext = _tracker;
        _ = _tracker.RefreshAsync();

        Populate();
        _sheet.Refresh();
    }

    private void Populate()
    {
        CharacterNameLabel.Text = _charDataService.CharacterName;
        PlayerNameLabel.Text = _charDataService.PlayerName;
        UpdateLevelLabels();

        RaceNameLabel.Text = _charDataService.CharacterRace;
        RaceFeatureNameLabel.Text = _charDataService.CharacterRaceFeatureName;
        RaceFeatureDescriptionLabel.Text = _charDataService.CharacterRaceFeatureDescription;

        ClassNameLabel.Text = _charDataService.CharacterClassName;

        BackgroundNameLabel.Text = _charDataService.SelectedBackgroundName;
        BackgroundDescriptionLabel.Text = _charDataService.SelectedBackgroundDescription;

        var hinderance = _charDataService.SelectedHinderance;
        if (hinderance != null)
        {
            HinderanceNameLabel.Text = hinderance.Name;
            HinderanceDescriptionLabel.Text = hinderance.Description;

            if (!string.IsNullOrEmpty(_charDataService.RewardStatName))
            {
                HinderanceRewardLabel.Text = $"Reward: +{_charDataService.RewardStatBonusAmount} {_charDataService.RewardStatName}";
                HinderanceRewardLabel.IsVisible = true;
            }
        }
        else
        {
            HinderanceNameLabel.Text = "None selected";
            HinderanceDescriptionLabel.Text = string.Empty;
        }

        LanguagesContainer.Children.Clear();
        if (_charDataService.SelectedLanguages == null || _charDataService.SelectedLanguages.Count == 0)
        {
            LanguagesContainer.Children.Add(new Label { Text = "None known", FontSize = 13 });
        }
        else
        {
            foreach (var language in _charDataService.SelectedLanguages)
            {
                LanguagesContainer.Children.Add(new Label { Text = language.Name, FontSize = 13, FontAttributes = FontAttributes.Bold });
                if (!string.IsNullOrWhiteSpace(language.Description))
                {
                    LanguagesContainer.Children.Add(new Label { Text = language.Description, FontSize = 11, TextColor = Colors.Gray });
                }
            }
        }

        StoryEditor.Text = _charDataService.CharacterStory;
        _storyDirty = false;
    }

    private void UpdateLevelLabels()
    {
        LevelLabel.Text = $"Level {_charDataService.CharacterLevel}" +
                          (_charDataService.PendingLevels > 0 ? $" · {_charDataService.PendingLevels} level-up(s) waiting" : string.Empty);
        ClassPicksLabel.Text = _charDataService.ClassPicksSummary;
        LevelUpButton.IsEnabled = _levelUp.CanLevelUp;   // single-player: always. GM-hosted: only while a level is granted.
    }

    private async void OnLevelUpClicked(object sender, EventArgs e)
    {
        var page = new LevelUpPage(_levelUp, _charDataService, _items, RefreshAfterLevelUpAsync);
        await Navigation.PushModalAsync(page);
    }

    // After a level: the level line, the skills table, the training pool and the weapon proficiencies all change.
    private async Task RefreshAfterLevelUpAsync()
    {
        UpdateLevelLabels();
        _sheet.Refresh();
        await _tracker.RefreshAsync();
    }

    private void OnStoryTextChanged(object sender, TextChangedEventArgs e)
    {
        _storyDirty = true;
    }

    private async void OnSaveStoryClicked(object sender, EventArgs e)
    {
        _charDataService.CharacterStory = StoryEditor.Text ?? string.Empty;
        await _charDataService.SaveNowAsync();
        _storyDirty = false;
        await DisplayAlertAsync("Saved", "Character story saved.", "OK");
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        bool pendingTraining = _sheet.HasDraft;

        if (_storyDirty || pendingTraining)
        {
            var pending = new List<string>();
            if (_storyDirty) pending.Add("the story");
            if (pendingTraining) pending.Add($"{_sheet.DraftTotal} training point(s) you haven't finished");

            string choice = await DisplayActionSheetAsync(
                $"You have unsaved changes: {string.Join(" and ", pending)}.", "Cancel", null, "Save and Close", "Discard and Close");

            if (choice == "Cancel" || choice == null) return;

            if (choice == "Save and Close")
            {
                if (_storyDirty)
                {
                    _charDataService.CharacterStory = StoryEditor.Text ?? string.Empty;
                    await _charDataService.SaveNowAsync();
                }
                if (pendingTraining) await _sheet.CommitDraftAsync();
            }
        }

        await Navigation.PopModalAsync();
    }

    // The dashboard refreshes once the sheet is gone (training points, the Bio button's flash).
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = _onClosed?.Invoke();
    }
}