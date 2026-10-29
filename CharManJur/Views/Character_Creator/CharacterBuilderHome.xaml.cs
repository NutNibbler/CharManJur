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

        lblHitProtection.Text = _charDataService.Hitpoints.ToString();

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

    // ===== ROLL FOR ME =====
    private void RefreshRoller()
    {
        var rolls = _charDataService.StatRolls;
        var pool = _charDataService.StatRollPool;
        bool rolled = rolls.Count > 0;

        RollButton.IsEnabled = !rolled;
        RollButton.Text = rolled ? "🎲 Already Rolled" : "🎲 Roll For Me";

        if (rolled)
        {
            var best = rolls.OrderByDescending(r => r).Take(4).ToList();
            RollResultLabel.Text = $"Rolled: {string.Join(", ", rolls)}\nBest four: {string.Join(", ", best)} (lowest dropped)";
        }
        else
        {
            RollResultLabel.Text = string.Empty;
        }

        PoolHintLabel.Text = !rolled ? "Roll to get your values, or type your own into the boxes."
            : pool.Count > 0 ? "Tap a value, then choose which ability gets it."
            : "All values assigned. You can still adjust the boxes by hand.";

        PoolHost.Children.Clear();
        foreach (int value in pool.OrderByDescending(v => v))
        {
            var chip = new Button
            {
                Text = value.ToString(),
                StyleId = value.ToString(),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                WidthRequest = 56,
                HeightRequest = 44,
                CornerRadius = 8,
                Margin = new Thickness(0, 0, 8, 8)
            };
            chip.SetDynamicResource(Button.BackgroundColorProperty, "ThemeSummaryButtonColor");
            chip.Clicked += OnPoolValueClicked;
            PoolHost.Children.Add(chip);
        }

        ResetPoolButton.IsVisible = rolled && pool.Count < 4;
    }

    private async void OnRollForMeClicked(object? sender, EventArgs e)
    {
        if (_charDataService.StatRolls.Count > 0) return;

        bool ok = await DisplayAlertAsync("Roll For Me",
            "This rolls three D6 five times and keeps the best four. You can't re-roll for this character. Roll now?",
            "Roll", "Cancel");
        if (!ok) return;

        var rolls = new List<int>();
        for (int i = 0; i < 5; i++)
            rolls.Add(Random.Shared.Next(1, 7) + Random.Shared.Next(1, 7) + Random.Shared.Next(1, 7));

        _charDataService.StatRolls = rolls;
        _charDataService.StatRollPool = rolls.OrderByDescending(r => r).Take(4).ToList();
        RefreshRoller();
    }

    private async void OnPoolValueClicked(object? sender, EventArgs e)
    {
        if (sender is not Button b || !int.TryParse(b.StyleId, out int value)) return;

        string? choice = await DisplayActionSheetAsync($"Assign {value} to...", "Cancel", null,
            "Vigor", "Agility", "Mind", "Spirit");
        if (choice == null || choice == "Cancel") return;

        switch (choice)
        {
            case "Vigor": entryStatVigor.Text = value.ToString(); break;
            case "Agility": entryStatAgility.Text = value.ToString(); break;
            case "Mind": entryStatMind.Text = value.ToString(); break;
            case "Spirit": entryStatSpirit.Text = value.ToString(); break;
        }

        _charDataService.StatRollPool.Remove(value);   // one use per rolled value
        RefreshRoller();
    }

    // Gives all four kept values back to the pool. It never rolls again.
    private void OnResetPoolClicked(object? sender, EventArgs e)
    {
        _charDataService.StatRollPool = _charDataService.StatRolls.OrderByDescending(r => r).Take(4).ToList();
        RefreshRoller();
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

        lblGuidelinesBody.Text = "  -  To obtain your ability scores, roll three D6 and combine them, five times. Then, choose the four BEST rolls to be the ability scores of your choosing.\n       -  Example: You rolled 15, 12, 8, 10, 5. You may assign 4 of these numbers to any Ability score you desire.\n       -  Vigor: Relates to physical prowess and fortitude\n       -  Agility: Relates to physical speed and reaction time\n       -  Mind: Relates to knowledge and intellect\n       -  Spirit: Relates to willpower and sociability\n       -  Hover over each attribute to view a quick description.  -  Your ASM is your Ability Score Modifier, this will take part in adjusting rolls in-game.\n  -  Your Hitpoints are calculated based on your Vigor ASM, and other choices such as Class.\n  -  Remember: All stats can be adjusted after creating your character, but they will be treated as adjustments from the original setup.";
        RefreshRoller();
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
        // Step 5: Update Hitpoints display
        lblHitProtection.Text = _charDataService.Hitpoints.ToString();
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