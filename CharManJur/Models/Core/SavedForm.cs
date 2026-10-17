using System;
using System.Collections.Generic;

namespace CharManJur.Models;

// A Shifter form the player saved to re-use: the dice it was rolled with, its traits, element picks and scores.
public class SavedForm
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PolymorphDice { get; set; } = 1;
    public List<string> Traits { get; set; } = new();
    public Dictionary<string, DamageElement> Elements { get; set; } = new();   // trait name -> chosen element
    public int Vigor { get; set; } = 10;
    public int Agility { get; set; } = 10;
    public DateTime SavedAt { get; set; } = DateTime.Now;
}