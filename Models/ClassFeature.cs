using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class ClassFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<SubFeatureType> UnlockableTypes { get; set; } = new();
}

public enum SubFeatureType
{
    Quip,
    Spell,
    Technique,
    Blueprint
}