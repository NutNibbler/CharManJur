using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CharManJur.Models;

public class Spell : INotifyPropertyChanged
{
    // === CUSTOM SPELL ID CONSTANTS ===
    public const int CUSTOM_SPELL_BASE = 50000;   // Base for custom spell IDs
    public const int CUSTOM_SPELL_START = 50001;  // First custom spell ID

    // === CUSTOM SPELL ATTRIBUTES ===
    public Guid Guid { get; set; } = Guid.Empty;  // permanent identity — never changes, this is what actually gets referenced

    private bool _isLoaded = true;
    public bool IsLoaded
    {
        get => _isLoaded;
        set
        {
            if (_isLoaded != value)
            {
                _isLoaded = value;
                OnPropertyChanged();
            }
        }
    }

    public string SourcePackId { get; set; } = "Local";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsPlayerCreated { get; set; } = false;

    public int Id { get; set; }

    // These are only consulted during character-creation-time filtering
    // (GetSpellsForCharacterAsync). A custom Spell created via the Asset Manager,
    // with no character context, will have both null — meaning it simply won't
    // appear during character creation. That's intentional: reaching it there is
    // out of scope for now. A future live-game unlock system will be the actual
    // path for Asset-Manager-created content to reach a character, and won't
    // consult these fields at all.
    public int? UnlockFeatureId { get; set; } = 19;
    public int? UnlockClassId { get; set; } = 0;

    public string SpellName { get; set; } = string.Empty;
    public string? SpellDescription { get; set; } = string.Empty;
    public string? SpellDice { get; set; } = string.Empty;
    public string? SpellRange { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}