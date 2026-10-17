using System.Collections.Generic;

namespace CharManJur.Models;

public class CreateCustomBackgroundRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VigorModifier { get; set; } = 0;
    public int AgilityModifier { get; set; } = 0;
    public int MindModifier { get; set; } = 0;
    public int SpiritModifier { get; set; } = 0;
    public List<BGSkillBonuses> SkillBonuses { get; set; } = new();
    public List<StartingItem> StartingItems { get; set; } = new();
    public List<ItemChoice> ItemChoices { get; set; } = new();
    public List<FamiliarChoice> FamiliarChoices { get; set; } = new();
}