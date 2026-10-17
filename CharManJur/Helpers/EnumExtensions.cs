using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CharManJur.Helpers;

public static class EnumExtensions
{
    /// Gets the enum value as a string (just the name).
    public static string GetDisplayName(this Enum enumValue)
    {
        return enumValue.ToString();
    }

    /// Gets a list of all enum values as strings.
    public static List<string> GetDisplayNames<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Select(e => e.ToString())
            .ToList();
    }

    /// Gets the Description from a [Display(Description = "...")] attribute on this
    /// enum member, if one is present. Returns empty string if the member has no
    /// Display attribute or no Description set — never null, so callers can bind
    /// straight to it without a null-check.
    public static string GetDescription(this Enum enumValue)
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
            .FirstOrDefault() as DisplayAttribute;
        return attribute?.Description ?? string.Empty;
    }
}