using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class BioPage : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private bool _storyDirty = false;

    // Visual grouping only, per the rulebook's skill table — not used for any calculation.
    private static readonly (string Stat, string[] Skills)[] StatSkillGroups =
    {
        ("Vigor", new[] { "Athletics", "Constitution", "Grapple", "Presence", "Ride" }),
        ("Agility", new[] { "Acrobatics", "Aim", "Drive", "Stealth", "Thief" }),
        ("Mind", new[] { "Arcana", "Artifice", "Heal", "Investigate", "Lore" }),
        ("Spirit", new[] { "Commune", "Deception", "Diplomacy", "Sight", "Survival" }),
    };

    // Red = reduced below its max (damage, an effect), green = above it.
    private static readonly Color PenaltyColor = Color.FromArgb("#e74c3c");
    private static readonly Color BonusColor = Color.FromArgb("#2ecc71");

    public BioPage(ICharAttribDataService charDataService)
    {
        InitializeComponent();
        _charDataService = charDataService;
        Populate();
    }

    private static Color ThemeColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;

    private static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();

    private void Populate()
    {
        CharacterNameLabel.Text = _charDataService.CharacterName;
        PlayerNameLabel.Text = _charDataService.PlayerName;

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

        PopulateStatsAndSkills();

        StoryEditor.Text = _charDataService.CharacterStory;
        _storyDirty = false;
    }

    private void PopulateStatsAndSkills()
    {
        StatsSkillsContainer.Children.Clear();

        foreach (var group in StatSkillGroups)
        {
            int max = group.Stat switch
            {
                "Vigor" => _charDataService.TotalStatVigor,
                "Agility" => _charDataService.TotalStatAgility,
                "Mind" => _charDataService.TotalStatMind,
                "Spirit" => _charDataService.TotalStatSpirit,
                _ => 0
            };
            int current = group.Stat switch
            {
                "Vigor" => _charDataService.CurrentStatVigor,
                "Agility" => _charDataService.CurrentStatAgility,
                "Mind" => _charDataService.CurrentStatMind,
                "Spirit" => _charDataService.CurrentStatSpirit,
                _ => 0
            };
            int maxAsm = group.Stat switch
            {
                "Vigor" => _charDataService.TotalASMStatVigor,
                "Agility" => _charDataService.TotalASMStatAgility,
                "Mind" => _charDataService.TotalASMStatMind,
                "Spirit" => _charDataService.TotalASMStatSpirit,
                _ => 0
            };
            int currentAsm = group.Stat switch
            {
                "Vigor" => _charDataService.CurrentASMStatVigor,
                "Agility" => _charDataService.CurrentASMStatAgility,
                "Mind" => _charDataService.CurrentASMStatMind,
                "Spirit" => _charDataService.CurrentASMStatSpirit,
                _ => 0
            };

            string valueText = current == max
                ? $"{max} (ASM {Signed(currentAsm)})"
                : $"{current} / {max} (ASM {Signed(currentAsm)}, max {Signed(maxAsm)})";
            Color valueColor = current < max ? PenaltyColor
                             : current > max ? BonusColor
                             : ThemeColor("ThemeDetailBorderColor1", Colors.LightSkyBlue);

            var statHeader = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            var statLabel = new Label { Text = group.Stat, FontSize = 15, FontAttributes = FontAttributes.Bold };
            var statValueLabel = new Label
            {
                Text = valueText,
                FontSize = 13,
                TextColor = valueColor,
                HorizontalOptions = LayoutOptions.End
            };
            Grid.SetColumn(statLabel, 0);
            Grid.SetColumn(statValueLabel, 1);
            statHeader.Children.Add(statLabel);
            statHeader.Children.Add(statValueLabel);

            // Column header row for the breakdown
            var columnHeaders = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(44)),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(40)),
                },
                Margin = new Thickness(8, 6, 0, 2)
            };
            string[] headerTexts = { "", "Nat", "ASM", "Trn", "Mod", "Tot" };
            for (int c = 0; c < headerTexts.Length; c++)
            {
                var h = new Label { Text = headerTexts[c], FontSize = 9, TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center };
                Grid.SetColumn(h, c);
                columnHeaders.Children.Add(h);
            }

            var skillsGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(44)),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(new GridLength(40)),
                },
                RowSpacing = 3,
                Margin = new Thickness(8, 0, 0, 0)
            };

            for (int i = 0; i < group.Skills.Length; i++)
            {
                skillsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                string skillName = group.Skills[i];
                var breakdown = _charDataService.GetSkillBreakdown(skillName);
                int skillMaxAsm = _charDataService.GetSkillMaxASM(skillName);

                var nameLabel = new Label { Text = skillName, FontSize = 12, VerticalOptions = LayoutOptions.Center };
                var natLabel = FormatCell(breakdown.NaturalNegative);
                var asmCell = BuildAsmCell(breakdown.ASM, skillMaxAsm);
                var trnLabel = FormatCell(breakdown.TrainingPoints);
                var modLabel = FormatCell(breakdown.OtherModifiers);

                // Total is red/green when effects or a reduced (or raised) stat change it from the baseline.
                int effectDirection = _charDataService.SkillDirection(skillName);
                int statDirection = Math.Sign(breakdown.ASM - skillMaxAsm);
                int dir = Math.Sign(effectDirection + statDirection);

                var totLabel = new Label
                {
                    Text = Signed(breakdown.Total),
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center
                };
                if (dir < 0) totLabel.TextColor = PenaltyColor;
                else if (dir > 0) totLabel.TextColor = BonusColor;

                Grid.SetRow(nameLabel, i); Grid.SetColumn(nameLabel, 0);
                Grid.SetRow(natLabel, i); Grid.SetColumn(natLabel, 1);
                Grid.SetRow(asmCell, i); Grid.SetColumn(asmCell, 2);
                Grid.SetRow(trnLabel, i); Grid.SetColumn(trnLabel, 3);
                Grid.SetRow(modLabel, i); Grid.SetColumn(modLabel, 4);
                Grid.SetRow(totLabel, i); Grid.SetColumn(totLabel, 5);

                skillsGrid.Children.Add(nameLabel);
                skillsGrid.Children.Add(natLabel);
                skillsGrid.Children.Add(asmCell);
                skillsGrid.Children.Add(trnLabel);
                skillsGrid.Children.Add(modLabel);
                skillsGrid.Children.Add(totLabel);
            }

            var groupLayout = new VerticalStackLayout { Spacing = 2 };
            groupLayout.Children.Add(statHeader);

            // Explain a reduced (or raised) stat in plain words.
            if (current != max)
            {
                int diff = Math.Abs(max - current);
                string direction = current < max ? "below" : "above";
                string note = currentAsm != maxAsm
                    ? $"{group.Stat} is {diff} {direction} its max, so these skills use ASM {Signed(currentAsm)} instead of {Signed(maxAsm)}."
                    : $"{group.Stat} is {diff} {direction} its max, but the modifier is unchanged.";
                groupLayout.Children.Add(new Label
                {
                    Text = note,
                    FontSize = 10,
                    TextColor = valueColor,
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            groupLayout.Children.Add(columnHeaders);
            groupLayout.Children.Add(skillsGrid);

            StatsSkillsContainer.Children.Add(groupLayout);
        }
    }

    private static Label FormatCell(int value)
    {
        var label = new Label
        {
            Text = Signed(value),
            FontSize = 11,
            HorizontalOptions = LayoutOptions.Center
        };
        if (value == 0) label.TextColor = Colors.Gray;
        return label;
    }

    // The skill's ASM from the stat's CURRENT value. When that differs from the max-based modifier, it is
    // shown red/green with the max underneath, so the effect of the reduced stat is visible.
    private static View BuildAsmCell(int current, int max)
    {
        if (current == max) return FormatCell(current);

        var layout = new VerticalStackLayout { Spacing = 0, HorizontalOptions = LayoutOptions.Center };
        layout.Children.Add(new Label
        {
            Text = Signed(current),
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = current < max ? PenaltyColor : BonusColor,
            HorizontalOptions = LayoutOptions.Center
        });
        layout.Children.Add(new Label
        {
            Text = $"max {Signed(max)}",
            FontSize = 8,
            TextColor = Colors.Gray,
            HorizontalOptions = LayoutOptions.Center
        });
        return layout;
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
        if (_storyDirty)
        {
            string choice = await DisplayActionSheetAsync(
                "You have unsaved changes to the story.", "Cancel", null, "Save and Close", "Discard and Close");

            if (choice == "Cancel" || choice == null) return;

            if (choice == "Save and Close")
            {
                _charDataService.CharacterStory = StoryEditor.Text ?? string.Empty;
                await _charDataService.SaveNowAsync();
            }
        }

        await Navigation.PopModalAsync();
    }
}