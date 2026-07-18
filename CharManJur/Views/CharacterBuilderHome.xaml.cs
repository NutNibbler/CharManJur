using CharManJur.Services;
using CharManJur.Models;  // For CharacterSaveData

namespace CharManJur.Views;

public partial class CharacterBuilderHome : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharacterPersistenceService _persistenceService;  // ← ADDED

    public CharacterBuilderHome(
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService,
        ICharacterPersistenceService persistenceService)  // ← ADDED
    {
        InitializeComponent();
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
        _persistenceService = persistenceService;  // ← ADDED
    }

    private void LoadDataFromService()
    {
        // Assign the global character service variables to local variables
        string campaignType = string.IsNullOrEmpty(_charDataService.CampaignType)
            ? "[Not Set]"
            : _charDataService.CampaignType;
        string characterName = string.IsNullOrEmpty(_charDataService.CharacterName)
            ? "[Not Set]"
            : _charDataService.CharacterName;
        string playerName = string.IsNullOrEmpty(_charDataService.PlayerName)
            ? "[Not Set]"
            : _charDataService.PlayerName;

        lblCampaignType.Text = $"Campaign Type: {campaignType}";
        lblCharacterName.Text = $"Character Name: {characterName}";
        lblPlayerName.Text = $"Player Name: {playerName}";
    }

    private void LoadStatsFromService()
    {
        System.Diagnostics.Debug.WriteLine($"=== LoadStatsFromService ===");
        System.Diagnostics.Debug.WriteLine($"Vigor: {_charDataService.StatVigor}");
        System.Diagnostics.Debug.WriteLine($"Agility: {_charDataService.StatAgility}");
        System.Diagnostics.Debug.WriteLine($"Mind: {_charDataService.StatMind}");
        System.Diagnostics.Debug.WriteLine($"Spirit: {_charDataService.StatSpirit}");
        System.Diagnostics.Debug.WriteLine($"HP: {_charDataService.Hitpoints}");

        // Load stats - handle null values
        entryStatVigor.Text = _charDataService.StatVigor?.ToString() ?? "";
        entryStatAgility.Text = _charDataService.StatAgility?.ToString() ?? "";
        entryStatMind.Text = _charDataService.StatMind?.ToString() ?? "";
        entryStatSpirit.Text = _charDataService.StatSpirit?.ToString() ?? "";
        entryStatHp.Text = _charDataService.Hitpoints?.ToString() ?? "";

        // Load modifiers
        lblVigorModifier.Text = _charDataService.ASMStatVigor.ToString();
        lblAgilityModifier.Text = _charDataService.ASMStatAgility.ToString();
        lblMindModifier.Text = _charDataService.ASMStatMind.ToString();
        lblSpiritModifier.Text = _charDataService.ASMStatSpirit.ToString();
    }

    private void RefreshUI()
    {
        LoadDataFromService();
        LoadStatsFromService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // ALWAYS load data from service first
        LoadDataFromService();
        LoadStatsFromService();

        if (_globalMenuDataService.NeedsUIReset == true)
        {
            System.Diagnostics.Debug.WriteLine("=== CharacterBuilderHome: NeedsUIReset ===");
            _globalMenuDataService.NeedsUIReset = false;
        }

        lblGuidelinesBody.Text = "  -  To obtain your ability scores, roll three D6 and combine them, five times. Then, choose the four BEST rolls to be the ability scores of your choosing.\n       -  Example: You rolled 15, 12, 8, 10, 5. You may assign 4 of these numbers to any Ability score you desire.\n       -  Vigor: Relates to physical prowess and fortitude\n       -  Agility: Relates to physical speed and reaction time\n       -  Mind: Relates to knowledge and intellect\n       -  Spirit: Relates to willpower and sociability\n       -  Hover over each attribute to view a quick description.  -  Your ASM is your Ability Score Modifier, this will take part in adjusting rolls in-game.\n  -  To obtain your HIT PROTECTION(HP), roll one D6, then combine your Vigor ASM.\n       -  Hit Protection may be affected by classes and features, more on this later.\n  -  Remember: All stats can be adjusted after creating your character.";
    }

    private async void OnVigorChanged(object sender, EventArgs e)
    {
        // Step 1: Get the text from the entry
        string inputText = entryStatVigor.Text;

        // Step 2: Validate and convert to integer
        if (string.IsNullOrWhiteSpace(inputText))
        {
            // If empty, reset to default
            _charDataService.StatVigor = 10;
            _charDataService.ASMStatVigor = _charDataService.GetAbilityModifier(10);
            return;
        }

        if (!int.TryParse(inputText, out int vigorScore))
        {
            // Invalid input - revert to previous valid value
            // Option 1: Reset to default
            entryStatVigor.Text = "10";
            _charDataService.StatVigor = 10;
            _charDataService.ASMStatVigor = _charDataService.GetAbilityModifier(10);

            // Option 2: Show error message
            await DisplayAlertAsync("Invalid Input", "Please enter a number between 1 and 20.", "OK");
            return;
        }

        // Step 3: Clamp to valid range (1-20)
        if (vigorScore < 1) vigorScore = 1;
        if (vigorScore > 20) vigorScore = 20;

        // Update the entry text if it was clamped
        if (vigorScore.ToString() != entryStatVigor.Text)
        {
            entryStatVigor.Text = vigorScore.ToString();
        }

        // Step 4: Save to service and calculate modifier
        _charDataService.StatVigor = vigorScore;
        _charDataService.ASMStatVigor = _charDataService.GetAbilityModifier(vigorScore);
        lblVigorModifier.Text = $"{_charDataService.ASMStatVigor}";
    }

    private async void OnAgilityChanged(object sender, EventArgs e)
    {
        // Step 1: Get the text from the entry
        string inputText = entryStatAgility.Text;

        // Step 2: Validate and convert to integer
        if (string.IsNullOrWhiteSpace(inputText))
        {
            // If empty, reset to default
            _charDataService.StatAgility = 10;
            _charDataService.ASMStatAgility = _charDataService.GetAbilityModifier(10);
            return;
        }

        if (!int.TryParse(inputText, out int agilityScore))
        {
            // Invalid input - revert to previous valid value
            // Option 1: Reset to default
            entryStatAgility.Text = "10";
            _charDataService.StatAgility = 10;
            _charDataService.ASMStatAgility = _charDataService.GetAbilityModifier(10);

            // Option 2: Show error message
            await DisplayAlertAsync("Invalid Input", "Please enter a number between 1 and 20.", "OK");
            return;
        }

        // Step 3: Clamp to valid range (1-20)
        if (agilityScore < 1) agilityScore = 1;
        if (agilityScore > 20) agilityScore = 20;

        // Update the entry text if it was clamped
        if (agilityScore.ToString() != entryStatAgility.Text)
        {
            entryStatAgility.Text = agilityScore.ToString();
        }

        // Step 4: Save to service and calculate modifier
        _charDataService.StatAgility = agilityScore;
        _charDataService.ASMStatAgility = _charDataService.GetAbilityModifier(agilityScore);
        lblAgilityModifier.Text = $"{_charDataService.ASMStatAgility}";
    }

    private async void OnMindChanged(object send, EventArgs e)
    {
        // Step 1: Get the text from the entry
        string inputText = entryStatMind.Text;

        // Step 2: Validate and convert to integer
        if (string.IsNullOrWhiteSpace(inputText))
        {
            // If empty, reset to default
            _charDataService.StatMind = 10;
            _charDataService.ASMStatMind = _charDataService.GetAbilityModifier(10);
            return;
        }

        if (!int.TryParse(inputText, out int mindScore))
        {
            // Invalid input - revert to previous valid value
            // Option 1: Reset to default
            entryStatMind.Text = "10";
            _charDataService.StatMind = 10;
            _charDataService.ASMStatMind = _charDataService.GetAbilityModifier(10);

            // Option 2: Show error message
            await DisplayAlertAsync("Invalid Input", "Please enter a number between 1 and 20.", "OK");
            return;
        }

        // Step 3: Clamp to valid range (1-20)
        if (mindScore < 1) mindScore = 1;
        if (mindScore > 20) mindScore = 20;

        // Update the entry text if it was clamped
        if (mindScore.ToString() != entryStatMind.Text)
        {
            entryStatMind.Text = mindScore.ToString();
        }

        // Step 4: Save to service and calculate modifier
        _charDataService.StatMind = mindScore;
        _charDataService.ASMStatMind = _charDataService.GetAbilityModifier(mindScore);
        lblMindModifier.Text = $"{_charDataService.ASMStatMind}";
    }

    private async void OnSpiritChanged(object send, EventArgs e)
    {
        // Step 1: Get the text from the entry
        string inputText = entryStatSpirit.Text;

        // Step 2: Validate and convert to integer
        if (string.IsNullOrWhiteSpace(inputText))
        {
            // If empty, reset to default
            _charDataService.StatSpirit = 10;
            _charDataService.ASMStatSpirit = _charDataService.GetAbilityModifier(10);
            return;
        }

        if (!int.TryParse(inputText, out int spiritScore))
        {
            // Invalid input - revert to previous valid value
            // Option 1: Reset to default
            entryStatSpirit.Text = "10";
            _charDataService.StatSpirit = 10;
            _charDataService.ASMStatSpirit = _charDataService.GetAbilityModifier(10);

            // Option 2: Show error message
            await DisplayAlertAsync("Invalid Input", "Please enter a number between 1 and 20.", "OK");
            return;
        }

        // Step 3: Clamp to valid range (1-20)
        if (spiritScore < 1) spiritScore = 1;
        if (spiritScore > 20) spiritScore = 20;

        // Update the entry text if it was clamped
        if (spiritScore.ToString() != entryStatSpirit.Text)
        {
            entryStatSpirit.Text = spiritScore.ToString();
        }

        // Step 4: Save to service and calculate modifier
        _charDataService.StatSpirit = spiritScore;
        _charDataService.ASMStatSpirit = _charDataService.GetAbilityModifier(spiritScore);
        lblSpiritModifier.Text = $"{_charDataService.ASMStatSpirit}";
    }

    private async void OnHpChanged(object send, EventArgs e)
    {
        // Step 1: Get the text from the entry
        string inputText = entryStatHp.Text;

        // Step 2: Validate and convert to integer
        if (string.IsNullOrWhiteSpace(inputText))
        {
            // If empty, reset to default
            _charDataService.Hitpoints = 1;
            return;
        }

        if (!int.TryParse(inputText, out int Hitpoints))
        {
            // Invalid input - revert to previous valid value
            // Option 1: Reset to default
            entryStatHp.Text = "1";
            _charDataService.Hitpoints = 1;

            // Option 2: Show error message
            await DisplayAlertAsync("Invalid Input", "Please enter a number.", "OK");
            return;
        }

        _charDataService.Hitpoints = Hitpoints;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CreateNewCharacter");
    }

    private async void OnKinClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_KinSelection");
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
            await Shell.Current.GoToAsync("///MainPage");
        }
        else
        {
            return;
        }
    }

    private async void OnSaveForLaterClicked(object sender, EventArgs e)
    {
        _charDataService.SetCurrentPage("///CharacterBuilderHome");

        string playerName = string.IsNullOrEmpty(_charDataService.PlayerName)
            ? "UnknownPlayer"
            : _charDataService.PlayerName;

        string characterName = string.IsNullOrEmpty(_charDataService.CharacterName)
            ? "UnknownCharacter"
            : _charDataService.CharacterName;

        string fileName = await _persistenceService.GenerateFileName(playerName, characterName);

        bool fileExists = await _persistenceService.CharacterExistsAsync(fileName);
        if (fileExists)
        {
            bool overrideFile = await DisplayAlertAsync(
                "File Exists",
                $"A character save named '{fileName}' already exists. Override it?",
                "Yes, Override",
                "No, Cancel");

            if (!overrideFile) return;
        }

        var saveData = _charDataService.CreateSaveData();
        saveData.FileName = fileName;
        saveData.LastSaved = DateTime.Now;

        bool success = await _persistenceService.SaveCharacterDataAsync(saveData);

        if (success)
        {
            _charDataService.MarkCharacterSaved();
            _charDataService.SaveFileName = fileName;

            await DisplayAlertAsync("Character Saved!",
                $"Your character '{characterName}' has been saved.\n" +
                $"Save ID: {fileName}",
                "OK");

            await Shell.Current.GoToAsync("///MainPage");
        }
        else
        {
            await DisplayAlertAsync("Error", "Failed to save character. Please try again.", "OK");
        }
    }
}