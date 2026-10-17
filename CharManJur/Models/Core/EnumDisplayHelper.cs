using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CharManJur.Models;

// Reads the [Display] names/descriptions on enums, with a readable fallback ("ToolKit" -> "Tool Kit").
public static class EnumDisplayHelper
{
    public static string GetName(Enum value)
        => GetAttribute(value)?.Name ?? Regex.Replace(value.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    public static string? GetDescription(Enum value) => GetAttribute(value)?.Description;

    private static DisplayAttribute? GetAttribute(Enum value)
        => value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>();

    // "Bleed V" means Bleed 5: the number in the name replaces the "X" in the description.
    public static string GetEffectDescription(WeaponEffectType effect)
    {
        string description = GetDescription(effect) ?? string.Empty;
        int? level = ParseTrailingRoman(GetName(effect));
        return level.HasValue ? Regex.Replace(description, @"\bX\b", level.Value.ToString()) : description;
    }

    private static int? ParseTrailingRoman(string name)
    {
        string token = name[(name.LastIndexOf(' ') + 1)..];
        if (token.Length == 0 || token.Any(c => c != 'I' && c != 'V' && c != 'X')) return null;

        int total = 0, highest = 0;
        foreach (char c in token.Reverse())
        {
            int v = c switch { 'I' => 1, 'V' => 5, _ => 10 };
            total += v < highest ? -v : v;
            highest = Math.Max(highest, v);
        }
        return total;
    }
}