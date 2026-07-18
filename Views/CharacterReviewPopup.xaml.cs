using CharManJur.Services;

namespace CharManJur.Views;

public partial class CharacterReviewPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;

    public CharacterReviewPopup(ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _charDataService = charDataService;

        // Set binding context to the service for sub-feature collections
        BindingContext = _charDataService;

        LoadCharacterData();
    }

    private void LoadCharacterData()
    {
        // Campaign Info
        lblCampaignType.Text = $"Campaign Type: {_charDataService.CampaignType}";
        lblPlayerName.Text = $"Player Name: {_charDataService.PlayerName}";
        lblCharacterName.Text = $"Character Name: {_charDataService.CharacterName}";

        // Ability Scores
        lblVigor.Text = $"Vigor: {_charDataService.StatVigor} (ASM: {_charDataService.ASMStatVigor:+0;-0;0})";
        lblAgility.Text = $"Agility: {_charDataService.StatAgility} (ASM: {_charDataService.ASMStatAgility:+0;-0;0})";
        lblMind.Text = $"Mind: {_charDataService.StatMind} (ASM: {_charDataService.ASMStatMind:+0;-0;0})";
        lblSpirit.Text = $"Spirit: {_charDataService.StatSpirit} (ASM: {_charDataService.ASMStatSpirit:+0;-0;0})";
        lblHp.Text = $"Hit Protection (HP): {_charDataService.Hitpoints}";

        // Race Information
        lblCharacterRace.Text = $"{_charDataService.CharacterRace}";
        lblCharacterRaceDescription.Text = $"{_charDataService.CharacterRaceDescription}";
        lblCharacterRaceFeatureName.Text = $"{_charDataService.CharacterRaceFeatureName}";
        lblCharacterRaceFeatureDescription.Text = $"{_charDataService.CharacterRaceFeatureDescription}";

        // Class Information
        lblCharacterClassName.Text = $"{_charDataService.CharacterClassName}";
        lblCharacterClassDescription.Text = $"{_charDataService.CharacterClassDescription}";
        lblCharacterClassFeatureName.Text = $"{_charDataService.CharacterClassFeatureName}";
        lblCharacterClassFeatureDescription.Text = $"{_charDataService.CharacterClassFeatureDescription}";

        // ===== FIXED: Renamed property =====
        lblCharacterClassAbilityScoreBonus.Text = $"{_charDataService.LevelUpAllocationRequirement}";

        // === NEW: Background Information ===
        lblBackgroundName.Text = $"Background: {_charDataService.SelectedBackgroundName}";
        lblBackgroundDescription.Text = _charDataService.SelectedBackgroundDescription;

        // Starting Items
        StartingItemsCollectionView.ItemsSource = _charDataService.SelectedStartingItems;

        // Skill Bonuses
        SkillBonusesCollectionView.ItemsSource = _charDataService.SelectedSkillBonuses;

        // Sub-features are bound directly via BindingContext
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        await Shell.Current.GoToAsync("///NextPage");
    }

    private async void OnGoBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}