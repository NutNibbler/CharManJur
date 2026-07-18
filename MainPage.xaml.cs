using CharManJur.Services;

namespace CharManJur;

public partial class MainPage : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;

    public MainPage(ICharAttribDataService charDataService, IGlobalMenuDataService globalMenuDataService)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
    }

    private async void OnCreateNewCharacterClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== Create New Character Clicked ===");

        // Clear any existing data
        _charDataService.ClearCharacterCreationData();

        // Set mode to character creation
        _globalMenuDataService.SetCharacterCreationMode(true);
        _globalMenuDataService.CharBuilderResetRequest();

        System.Diagnostics.Debug.WriteLine($"=== IsInCharacterCreation set to: {_globalMenuDataService.IsInCharacterCreation} ===");

        await Shell.Current.GoToAsync("///CreateNewCharacter", true);
    }

    private async void OnLoadCharacterClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== Load Character Clicked ===");
        _globalMenuDataService.SetCharacterCreationMode(true);
        await Shell.Current.GoToAsync("///LoadCharacter");
    }

    private async void OnResumeCharacterClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== Resume Character Clicked ===");
        _globalMenuDataService.SetCharacterCreationMode(true);
        await Shell.Current.GoToAsync("///LoadCharacter");
    }

    private async void OnOfflineCampaignClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///OfflineCampaignHome");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Settings",
            "App settings will go here.",
            "OK");
    }
}