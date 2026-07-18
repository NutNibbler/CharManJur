using Microsoft.Maui.Controls;
using CharManJur.Services;

namespace CharManJur.Views;

public partial class CreateNewCharacter : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;

    public CreateNewCharacter(ICharAttribDataService charDataService, IGlobalMenuDataService globalMenuDataService)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
        LoadCampaignTypes();
    }

    private void LoadCampaignTypes()
    {
        // Populate the picker from global data
        pickerCampaignType.ItemsSource = _charDataService.CampaignTypes;
    }

    private void LoadDataFromService()
    {
        entryCharName.Text = _charDataService.CharacterName ?? "";
        entryPlayerName.Text = _charDataService.PlayerName ?? "";

        if (!string.IsNullOrEmpty(_charDataService.CampaignType))
        {
            pickerCampaignType.SelectedItem = _charDataService.CampaignType;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine($"=== CreateNewCharacter OnAppearing: NeedsUIReset={_globalMenuDataService.NeedsUIReset} ===");

        if (_globalMenuDataService.NeedsUIReset == true)
        {
            // Clear the UI fields
            entryCharName.Text = string.Empty;
            entryPlayerName.Text = string.Empty;
            pickerCampaignType.SelectedItem = null;
            pickerCampaignType.SelectedIndex = -1;

            // Reset the flag
            _globalMenuDataService.NeedsUIReset = false;
        }
        else
        {
            // Load data from service if available
            entryCharName.Text = _charDataService.CharacterName ?? "";
            entryPlayerName.Text = _charDataService.PlayerName ?? "";

            if (!string.IsNullOrEmpty(_charDataService.CampaignType))
            {
                pickerCampaignType.SelectedItem = _charDataService.CampaignType;
            }
        }
    }

    private async void OnCreateButtonClicked(object sender, EventArgs e)
    {
        string charName = entryCharName.Text?.Trim();
        string playerName = entryPlayerName.Text?.Trim();
        string campaignType = pickerCampaignType.SelectedItem?.ToString() ?? "";
        bool validSetup = true;
        string errorMessage = "Please fill the following fields:\n\n";

        if (string.IsNullOrWhiteSpace(campaignType))
        {
            errorMessage += "• Campaign Type\n";
            validSetup = false;
        }

        if (string.IsNullOrWhiteSpace(playerName))
        {
            errorMessage += "• Player Name\n";
            validSetup = false;
        }

        if (string.IsNullOrWhiteSpace(charName))
        {
            errorMessage += "• Character Name\n";
            validSetup = false;
        }

        if (!validSetup)
        {
            await DisplayAlertAsync("Missing Information", errorMessage, "OK");
            return;
        }

        _charDataService.CampaignType = campaignType;
        _charDataService.PlayerName = playerName;
        _charDataService.CharacterName = charName;

        await Shell.Current.GoToAsync("///CharacterBuilderHome", true);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        bool confirmCancel = await DisplayAlertAsync(
            "Cancel New Character?",
            "Are you sure you want to cancel this character? All data will be lost!",
            "Yes",
            "No");

        if (confirmCancel)
        {
            _globalMenuDataService.CharBuilderResetRequest();
            _charDataService.ClearCharacterCreationData();
            await Shell.Current.GoToAsync("///MainPage", true);
        }
        else
        {
            return;
        }
    }
}