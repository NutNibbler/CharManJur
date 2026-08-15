using CharManJur.Services;
using CharManJur.ViewModels;
using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace CharManJur.Views;

public partial class Godrick_Training_Popup : ContentPage
{
    private readonly TrainingPopupViewModel _viewModel;
    private readonly ICharAttribDataService _charDataService;
    private readonly IGlobalMenuDataService _globalMenuDataService;
    private readonly ICharacterPersistenceService _persistenceService;

    private Dictionary<string, (Stepper Stepper, Label UsedLabel, Label TotalLabel)> _skillControls = new();

    public Godrick_Training_Popup(
        TrainingPopupViewModel viewModel,
        ICharAttribDataService charDataService,
        IGlobalMenuDataService globalMenuDataService,
        ICharacterPersistenceService persistenceService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _charDataService = charDataService;
        _globalMenuDataService = globalMenuDataService;
        _persistenceService = persistenceService;

        BindingContext = _viewModel;

        BuildSkillControls();
        UpdateAllTotals();
    }

    private void BuildSkillControls()
    {
        var skillGroups = new Dictionary<string, (string ContainerName, List<string> Skills)>
        {
            { "Vigor", ("VigorSkillsContainer", new List<string> { "Athletics", "Constitution", "Grapple", "Presence", "Ride" }) },
            { "Agility", ("AgilitySkillsContainer", new List<string> { "Acrobatics", "Aim", "Drive", "Stealth", "Thief" }) },
            { "Mind", ("MindSkillsContainer", new List<string> { "Arcana", "Artifice", "Heal", "Investigate", "Lore" }) },
            { "Spirit", ("SpiritSkillsContainer", new List<string> { "Commune", "Deception", "Diplomacy", "Sight", "Survival" }) }
        };

        foreach (var group in skillGroups)
        {
            var container = FindByName(group.Value.ContainerName) as VerticalStackLayout;
            if (container == null) continue;

            foreach (var skillName in group.Value.Skills)
            {
                var row = CreateSkillRow(skillName);
                container.Children.Add(row);
            }
        }
    }

    private Grid CreateSkillRow(string skillName)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(0.20, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.10, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.12, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.12, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.12, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.10, GridUnitType.Star) },
            new ColumnDefinition { Width = new GridLength(0.14, GridUnitType.Star) }
        },
            ColumnSpacing = 3
        };

        // Skill Name
        grid.Add(new Label
        {
            Text = skillName,
            FontSize = 14,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center
        }, 0, 0);

        // Base (-2) - ALWAYS -2
        grid.Add(new Label
        {
            Text = "-2",
            FontSize = 13,
            TextColor = Colors.Gray,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        }, 1, 0);

        // ASM
        var asmLabel = new Label
        {
            FontSize = 13,
            TextColor = Colors.Orange,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        asmLabel.SetBinding(Label.TextProperty, new Binding($"TotalASMStat{GetStatName(skillName)}"));
        grid.Add(asmLabel, 2, 0);

        // ===== UPDATED: Creation Bonus (Background + Race) =====
        var creationBonus = _viewModel.GetCreationBonusForSkill(skillName);
        var creationLabel = new Label
        {
            Text = creationBonus != 0 ? $"{creationBonus:+0;-0}" : "0",
            FontSize = 13,
            TextColor = Colors.DeepSkyBlue,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        grid.Add(creationLabel, 3, 0);

        // Training (Stepper) - Maximum is dynamic based on available points
        var stepper = new Stepper
        {
            Minimum = 0,
            Maximum = 10, // Set high, but will be limited by available points in the ViewModel
            Increment = 1,
            HorizontalOptions = LayoutOptions.Center,
            WidthRequest = 100
        };
        stepper.SetBinding(Stepper.ValueProperty, new Binding($"SkillTrainingLevels[{skillName}]", BindingMode.TwoWay));
        stepper.ValueChanged += OnStepperValueChanged;
        grid.Add(stepper, 4, 0);

        // Used (Training points used on this skill) - Shows pure training points
        var usedLabel = new Label
        {
            FontSize = 13,
            TextColor = Colors.DeepSkyBlue,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Text = "0"
        };
        usedLabel.SetBinding(Label.TextProperty, new Binding($"SkillTrainingLevels[{skillName}]"));
        grid.Add(usedLabel, 5, 0);

        // Total - Calculates -2 + training + ASM + Creation Bonus
        var totalLabel = new Label
        {
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.LightGreen,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Text = "0"
        };
        totalLabel.SetBinding(Label.TextProperty, new Binding($"SkillTotal_{skillName}", BindingMode.OneWay));
        grid.Add(totalLabel, 6, 0);

        _skillControls[skillName] = (stepper, usedLabel, totalLabel);

        return grid;
    }

    private string GetStatName(string skillName)
    {
        return skillName switch
        {
            "Athletics" or "Constitution" or "Grapple" or "Presence" or "Ride" => "Vigor",
            "Acrobatics" or "Aim" or "Drive" or "Stealth" or "Thief" => "Agility",
            "Arcana" or "Artifice" or "Heal" or "Investigate" or "Lore" => "Mind",
            "Commune" or "Deception" or "Diplomacy" or "Sight" or "Survival" => "Spirit",
            _ => "Vigor"
        };
    }

    private void UpdateUsedLabels()
    {
        foreach (var kvp in _skillControls)
        {
            string skillName = kvp.Key;
            var controls = kvp.Value;

            if (_viewModel.SkillTrainingLevels.ContainsKey(skillName))
            {
                controls.UsedLabel.Text = _viewModel.SkillTrainingLevels[skillName].ToString();
            }
        }
    }

    private void OnStepperValueChanged(object sender, ValueChangedEventArgs e)
    {
        var stepper = sender as Stepper;
        if (stepper == null) return;

        // Find which skill this stepper belongs to
        foreach (var kvp in _skillControls)
        {
            if (kvp.Value.Stepper == stepper)
            {
                string skillName = kvp.Key;
                int value = (int)stepper.Value;

                // Update the Used label immediately
                kvp.Value.UsedLabel.Text = value.ToString();

                // Update the ViewModel
                _viewModel.UpdateTrainingLevel(skillName, value);
                break;
            }
        }

        _viewModel.CalculateUsedPoints();
        UpdateAllTotals();

        // ===== FIX: UPDATE AVAILABLE POINTS DISPLAY =====
        // Force update the available points label
        lblAvailablePoints.Text = $"Available Points: {_viewModel.AvailablePoints}";

        // Also update the color based on available points
        if (_viewModel.AvailablePoints <= 0)
        {
            lblAvailablePoints.TextColor = Colors.Red;
        }
        else
        {
            lblAvailablePoints.TextColor = Colors.White;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        System.Diagnostics.Debug.WriteLine("=== Training Popup OnAppearing ===");

        // Force a complete recalculation from the service
        _viewModel.RecalculateAvailablePoints();
        _viewModel.CalculateUsedPoints();

        // Update all UI elements
        UpdateAllTotals();
        UpdateUsedLabels();

        // ===== FIX: UPDATE AVAILABLE POINTS DISPLAY =====
        lblAvailablePoints.Text = $"Available Points: {_viewModel.AvailablePoints}";

        // Update color based on available points
        if (_viewModel.AvailablePoints <= 0)
        {
            lblAvailablePoints.TextColor = Colors.Red;
        }
        else
        {
            lblAvailablePoints.TextColor = Colors.White;
        }

        System.Diagnostics.Debug.WriteLine($"Available Points: {_viewModel.AvailablePoints}");
        System.Diagnostics.Debug.WriteLine($"Total Available Points: {_viewModel.TotalAvailablePoints}");
    }

    private void UpdateAllTotals()
    {
        foreach (var kvp in _skillControls)
        {
            string skillName = kvp.Key;
            var controls = kvp.Value;

            int total = _viewModel.GetSkillTotal(skillName);
            controls.TotalLabel.Text = total.ToString();
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CharBuilder_Godrick_HinderanceSelection");
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        bool confirmComplete = await DisplayAlertAsync(
            "⚠️ Complete Character Creation",
            "This will finalize your character. You will not be able to return to the character creation menu for this character.\n\n" +
            "All character data will be saved to your character file.\n\n" +
            "Do you wish to proceed?",
            "Yes, Complete Character",
            "No, Go Back");

        if (!confirmComplete) return;

        await _viewModel.SaveCharacterAsync();

        _charDataService.MarkCharacterComplete();
        _charDataService.CurrentPage = "CharacterComplete";

        string fileName = await _persistenceService.GenerateFileName(
            _charDataService.PlayerName,
            _charDataService.CharacterName);

        var saveData = _charDataService.CreateSaveData();
        saveData.FileName = fileName;
        saveData.LastSaved = DateTime.Now;
        saveData.CurrentPage = "CharacterComplete";
        saveData.IsComplete = true;

        bool success = await _persistenceService.SaveCharacterDataAsync(saveData);

        if (success)
        {
            _charDataService.MarkCharacterSaved();
            _charDataService.SaveFileName = fileName;

            await DisplayAlertAsync(
                "✅ Character Complete!",
                $"Your character '{_charDataService.CharacterName}' has been successfully created and saved!",
                "OK");

            await Shell.Current.GoToAsync("///MainPage");
        }
        else
        {
            await DisplayAlertAsync("Error", "Failed to save character. Please try again.", "OK");
        }
    }

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        await _viewModel.SaveCharacterAsync();
        await Navigation.PopModalAsync();
    }

    private async void OnAwardTrainingPointClicked(object sender, EventArgs e)
    {
        string[] skills = new string[]
        {
            "Athletics", "Constitution", "Grapple", "Presence", "Ride",
            "Acrobatics", "Aim", "Drive", "Stealth", "Thief",
            "Arcana", "Artifice", "Heal", "Investigate", "Lore",
            "Commune", "Deception", "Diplomacy", "Sight", "Survival"
        };

        string selected = await DisplayActionSheetAsync(
            "Award Training Point",
            "Cancel",
            null,
            skills);

        if (selected != null && selected != "Cancel")
        {
            if (_viewModel.SkillTrainingLevels.ContainsKey(selected))
            {
                _viewModel.UpdateTrainingLevel(selected, _viewModel.SkillTrainingLevels[selected] + 1);
                UpdateAllTotals();
                await DisplayAlertAsync("Training Point Awarded", $"+1 Training Point awarded to {selected}", "OK");
            }
        }
    }
}