using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CharManJur.Models;

public class AssetPack
{
    public string PackId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Item> Items { get; set; } = new();
    public List<CharacterBackground> Backgrounds { get; set; } = new();
    public List<Familiar> Familiars { get; set; } = new();
    public List<Race> Races { get; set; } = new();
    public List<Hinderance> Hinderances { get; set; } = new();
    public List<Blueprint> Blueprints { get; set; } = new();
    public List<Spell> Spells { get; set; } = new();
    public List<Quip> Quips { get; set; } = new();
    public List<Technique> Techniques { get; set; } = new();
    public List<CustomTrait> Traits { get; set; } = new();
}

public enum PackAssetSyncMode
{
    None,
    OverwriteOnly,
    AddOverwrite
}

public class InstalledPackEntry : INotifyPropertyChanged
{
    public string PackId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Details { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsLoaded { get; set; } = true;
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    private int _itemCount;
    [JsonIgnore]
    public int ItemCount
    {
        get => _itemCount;
        set { _itemCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _backgroundCount;
    [JsonIgnore]
    public int BackgroundCount
    {
        get => _backgroundCount;
        set { _backgroundCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _familiarCount;
    [JsonIgnore]
    public int FamiliarCount
    {
        get => _familiarCount;
        set { _familiarCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _raceCount;
    [JsonIgnore]
    public int RaceCount
    {
        get => _raceCount;
        set { _raceCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _hinderanceCount;
    [JsonIgnore]
    public int HinderanceCount
    {
        get => _hinderanceCount;
        set { _hinderanceCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    // NEW: the four sub-feature types. Only surfaced today via SettingsPage.xaml's
    // pack list (AssetCountDisplay below).
    private int _blueprintCount;
    [JsonIgnore]
    public int BlueprintCount
    {
        get => _blueprintCount;
        set { _blueprintCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _spellCount;
    [JsonIgnore]
    public int SpellCount
    {
        get => _spellCount;
        set { _spellCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _quipCount;
    [JsonIgnore]
    public int QuipCount
    {
        get => _quipCount;
        set { _quipCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _techniqueCount;
    [JsonIgnore]
    public int TechniqueCount
    {
        get => _techniqueCount;
        set { _techniqueCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private int _traitCount;
    [JsonIgnore]
    public int TraitCount
    {
        get => _traitCount;
        set { _traitCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(AssetCountDisplay)); }
    }

    private bool _isExpanded;
    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string AssetCountDisplay =>
    $"{ItemCount} item(s), {BackgroundCount} background(s), {FamiliarCount} familiar(s), {RaceCount} race(s), {HinderanceCount} hinderance(s), " +
    $"{BlueprintCount} blueprint(s), {SpellCount} spell(s), {QuipCount} quip(s), {TechniqueCount} technique(s), {TraitCount} trait(s)";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}