using CharManJur.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CharManJur.Models;

public class CharacterBackground
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // === ABILITY SCORE MODIFIERS ===
    public int VigorModifier { get; set; } = 0;
    public int AgilityModifier { get; set; } = 0;
    public int MindModifier { get; set; } = 0;
    public int SpiritModifier { get; set; } = 0;

    // === SKILL MODIFIERS ===
    public List<BGSkillBonuses> SkillBonuses { get; set; } = new();

    public ObservableCollection<StartingItem>? StartingItems { get; set; } = new();
    public List<ItemChoice> ItemChoices { get; set; } = new();
    public Familiar? StartingFamiliar { get; set; }
    public List<FamiliarChoice> FamiliarChoices { get; set; } = new();

    // === HELPER METHODS ===
    public int GetAbilityModifier(string statName)
    {
        return statName.ToLower() switch
        {
            "vigor" => VigorModifier,
            "agility" => AgilityModifier,
            "mind" => MindModifier,
            "spirit" => SpiritModifier,
            _ => 0
        };
    }

    public int GetSkillModifier(string skillName)
    {
        var modifier = SkillBonuses.FirstOrDefault(s => s.SkillName == skillName);
        return modifier?.Bonus ?? 0;
    }
}

public class StartingItem : INotifyPropertyChanged
{
    private Item? _itemDetails;

    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public string? PlayerNote { get; set; }

    public Item? ItemDetails
    {
        get => _itemDetails;
        set
        {
            if (_itemDetails != value)
            {
                _itemDetails = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class BGSkillBonuses
{
    public string SkillName { get; set; } = string.Empty;
    public int Bonus { get; set; }
}

public class ItemChoice
{
    public int Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RecommendedMin { get; set; } = 0;
    public int RecommendedMax { get; set; } = 0;
    public bool AllowDuplicates { get; set; } = false;
    public int MaxDuplicatesPerItem { get; set; } = 1;
    public bool AllowCustomItems { get; set; } = true;
    public string? CustomItemCategory { get; set; }
    public ItemQueryCriteria QueryCriteria { get; set; } = new();
}

public class ItemQueryCriteria
{
    public ItemCategory? Category { get; set; }
    public List<ItemCategory>? AllowedCategories { get; set; }
    public List<int>? SpecificItemIds { get; set; }
    public string? SearchTerm { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public int? MaxRarity { get; set; }
    public bool IncludePlayerCreated { get; set; } = true;
    public bool IncludeFoundation { get; set; } = true;
    public string? CampaignId { get; set; }
    public ItemSize? Size { get; set; }
    public List<ItemSize>? AllowedSizes { get; set; }
    public int? MaxResults { get; set; }
    public bool ExcludeItemsWithUses { get; set; } = false;
    public bool HasCategoryFilter => Category != null || (AllowedCategories != null && AllowedCategories.Any());
}

public class CharacterItemChoiceResult
{
    public int ChoiceId { get; set; }
    public string ChoicePrompt { get; set; } = string.Empty;
    public List<SelectedItemChoice> SelectedItems { get; set; } = new();
}

public class SelectedItemChoice
{
    public int ItemId { get; set; }
    public string? ItemName { get; set; }
    public int Quantity { get; set; } = 1;
    public string? PlayerNote { get; set; }
    public bool IsCustomItem { get; set; }
    public int? CustomItemTemplateId { get; set; }
}

public class FamiliarChoice
{
    public int Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RecommendedMin { get; set; } = 1;
    public int RecommendedMax { get; set; } = 1;
    public bool AllowCustomFamiliar { get; set; } = true;
    public FamiliarQueryCriteria QueryCriteria { get; set; } = new();
}

public class FamiliarQueryCriteria
{
    public string? Species { get; set; }
    public List<string>? AllowedSpecies { get; set; }
    public string? Size { get; set; }
    public List<string>? AllowedSizes { get; set; }
    public string? Intelligence { get; set; }
    public List<string>? AllowedIntelligences { get; set; }
    public bool IncludePlayerCreated { get; set; } = true;
    public bool IncludeFoundation { get; set; } = true;
    public string? CampaignId { get; set; }
    public List<int>? SpecificFamiliarIds { get; set; }
    public int? MaxResults { get; set; }
    public bool HasFilters => Species != null ||
                              (AllowedSpecies != null && AllowedSpecies.Any()) ||
                              Size != null ||
                              (AllowedSizes != null && AllowedSizes.Any()) ||
                              Intelligence != null ||
                              (AllowedIntelligences != null && AllowedIntelligences.Any());
}

public class ItemChoiceDisplay
{
    public ItemChoice Choice { get; set; } = new();
    public List<SelectableItem> Options { get; set; } = new();
}