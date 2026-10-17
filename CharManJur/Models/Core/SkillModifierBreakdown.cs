namespace CharManJur.Models;

public class SkillModifierBreakdown
{
    public string SkillName { get; set; } = string.Empty;
    public int NaturalNegative { get; set; }
    public int ASM { get; set; }
    public int TrainingPoints { get; set; }
    public int OtherModifiers { get; set; }   // Background + Race + Hinderance + Hinderance Reward + Effects
    public int Total => NaturalNegative + ASM + TrainingPoints + OtherModifiers;
}