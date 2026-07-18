using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur.Models;

public class Quip
{
    public int Id { get; set; }
    public int? UnlockFeatureId { get; set; } = 0;
    public int? UnlockClassId { get; set; } = 3;

    public string QuipName { get; set; } = string.Empty;
    public string? QuipDescription { get; set; } = string.Empty;
}
