using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CharManJur.Models;

public class Blueprint : INotifyPropertyChanged
{
    // === CUSTOM BLUEPRINT ID CONSTANTS ===
    public const int CUSTOM_BLUEPRINT_BASE = 60000;   // Base for custom blueprint IDs
    public const int CUSTOM_BLUEPRINT_START = 60001;  // First custom blueprint ID

    // === CUSTOM BLUEPRINT ATTRIBUTES ===
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
    public int BlueprintCost { get; set; }

    // These are only consulted during character-creation-time filtering
    // (GetBlueprintsForCharacterAsync). A custom Blueprint created via the Asset
    // Manager, with no character context, will have both null — meaning it simply
    // won't appear during character creation. That's intentional: reaching it there
    // is out of scope for now. A future live-game unlock system will be the actual
    // path for Asset-Manager-created content to reach a character, and won't consult
    // these fields at all.
    public int? UnlockFeatureId { get; set; } = 27;
    public int? UnlockClassId { get; set; } = 5;

    public string BlueprintName { get; set; } = string.Empty;
    public string BlueprintDescription { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}