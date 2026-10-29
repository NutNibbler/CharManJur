using System.Text.RegularExpressions;

namespace CharManJur.Models;

// What a Magic Die roll amounts to, per the rulebook:
//  - every die showing 4-6 gives 1 Fatigue (technique dice included);
//  - a SERIES (dice that match each other) is a backlash: the sum of the matching dice hurts the caster,
//    and 4 or more makes it a blast;
//  - [dice] is the number of dice rolled and [sum] adds up only the spell dice (technique dice never add to it);
//  - a cantrip does [sum] + [dice].
public sealed class MagicRollResult
{
    public IReadOnlyList<int> Dice { get; init; } = Array.Empty<int>();
    public IReadOnlyList<int> TechniqueDice { get; init; } = Array.Empty<int>();
    public int DiceCount => Dice.Count;
    public int Sum { get; init; }
    public int Fatigue { get; init; }
    public int SeriesDamage { get; init; }
    public string SeriesText { get; init; } = string.Empty;
    public bool HasSeries => SeriesDamage > 0;
    public bool SeriesIsBlast => SeriesDamage >= 4;
    public int CantripDamage => Sum + DiceCount;
}

public static class MagicDice
{
    public const int MaxDice = 20;

    public static List<int> Roll(int count)
        => Enumerable.Range(0, Math.Clamp(count, 1, MaxDice)).Select(_ => Random.Shared.Next(1, 7)).ToList();

    // Picks `count` different positions out of `total` dice, at random: which die powers which technique when the app rolls.
    // The result is in random order, so the first position goes to the first technique, and so on.
    public static List<int> PickRandomIndices(int total, int count)
    {
        var all = Enumerable.Range(0, Math.Max(0, total)).ToList();
        for (int i = all.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }
        return all.Take(Math.Clamp(count, 0, all.Count)).ToList();
    }

    // "4 3 2" (any separators) -> [4, 3, 2]. False with a message when it isn't exactly expectedCount dice of 1-6.
    public static bool TryParse(string? text, int expectedCount, out List<int> dice, out string error)
    {
        dice = new List<int>();
        error = string.Empty;

        foreach (Match m in Regex.Matches(text ?? string.Empty, @"\d+"))
        {
            if (!int.TryParse(m.Value, out int value) || value < 1 || value > 6)
            {
                error = "Every die must be a number from 1 to 6, separated by spaces (e.g. 4 3 2).";
                return false;
            }
            dice.Add(value);
        }

        if (dice.Count != expectedCount)
        {
            error = $"Enter exactly {expectedCount} dice, e.g. \"4 3 2\" (you entered {dice.Count}).";
            return false;
        }
        return true;
    }

    public static MagicRollResult Evaluate(IReadOnlyList<int> dice, IReadOnlyList<bool>? techniqueDice = null)
    {
        int sum = 0;
        var tech = new List<int>();
        for (int i = 0; i < dice.Count; i++)
        {
            bool isTechnique = techniqueDice != null && i < techniqueDice.Count && techniqueDice[i];
            if (isTechnique) tech.Add(dice[i]); else sum += dice[i];
        }

        var matching = dice.GroupBy(d => d).Where(g => g.Count() >= 2).OrderBy(g => g.Key).ToList();

        return new MagicRollResult
        {
            Dice = dice.ToList(),
            TechniqueDice = tech,
            Sum = sum,
            Fatigue = dice.Count(d => d >= 4),
            SeriesDamage = matching.Sum(g => g.Key * g.Count()),
            SeriesText = string.Join(", ", matching.Select(g => string.Join("+", Enumerable.Repeat(g.Key, g.Count()))))
        };
    }

    // Spell text uses [dice] and [sum]; fill in the real numbers once the dice are rolled.
    public static string FillPlaceholders(string? description, int dice, int sum)
    {
        string text = Regex.Replace(description ?? string.Empty, @"\[dice\]", dice.ToString(), RegexOptions.IgnoreCase);
        return Regex.Replace(text, @"\[sum\]", sum.ToString(), RegexOptions.IgnoreCase);
    }
}