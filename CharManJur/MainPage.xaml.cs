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

    // Solo Session: pick a saved character and play it without a GM (the old Load Character).
    private async void OnSoloSessionClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== Solo Session Clicked ===");
        _globalMenuDataService.SetCharacterCreationMode(true);
        await Shell.Current.GoToAsync("///LoadCharacter");
    }

    // Join a GM's session: pick a character, then an invite code or a session found on the network.
    private async void OnJoinSessionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///JoinSession");
    }

    private async void OnCreateCampaignClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CampaignCreatorPage?campaignId=new");
    }

    private async void OnResumeCampaignClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CampaignListPage");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///SettingsPage");
    }
}