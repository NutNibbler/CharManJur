using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CharManJur.Models;

public class Technique : INotifyPropertyChanged
{
    // === CUSTOM TECHNIQUE ID CONSTANTS ===
    public const int CUSTOM_TECHNIQUE_BASE = 30000;   // Base for custom technique IDs
    public const int CUSTOM_TECHNIQUE_START = 30001;  // First custom technique ID

    // === CUSTOM TECHNIQUE ATTRIBUTES ===
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
    // (GetTechniquesForCharacterAsync). A custom Technique created via the Asset
    // Manager, with no character context, defaults UnlockFeatureId to 0 (this field
    // is non-nullable, unlike the other three sub-feature types) and UnlockClassId to
    // null — meaning it simply won't appear during character creation unless a
    // context happens to have featureId 0. That's intentional: reaching it there is
    // out of scope for now. A future live-game unlock system will be the actual path
    // for Asset-Manager-created content to reach a character, and won't consult these
    // fields at all.
    public int UnlockFeatureId { get; set; } = 20;
    public int? UnlockClassId { get; set; } = 0;

    public string TechniqueName { get; set; } = string.Empty;
    public string TechniqueDescription { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}