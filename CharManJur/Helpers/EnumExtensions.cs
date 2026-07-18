using System;
using System.Collections.Generic;
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
}