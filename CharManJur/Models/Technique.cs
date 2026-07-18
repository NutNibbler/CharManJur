using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class Technique
{
    public int Id { get; set; }
    public int UnlockFeatureId { get; set; } = 20;
    public int? UnlockClassId { get; set; } = 0;

    public string TechniqueName { get; set; } = string.Empty;
    public string TechniqueDescription { get; set; } = string.Empty;

}
