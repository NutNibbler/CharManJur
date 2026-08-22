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
        set { _itemCount = value; OnPropertyChanged(); }
    }

    private bool _isExpanded;
    [JsonIgnore]
    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}