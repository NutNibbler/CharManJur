using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace CharManJur.Models;

public class InventoryItemDisplay : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isDropMode;

    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    private bool _isEmpty = false;
    public bool IsEmpty
    {
        get => _isEmpty;
        set
        {
            if (_isEmpty == value) return;
            _isEmpty = value;
            OnPropertyChanged();
        }
    }
    public bool IsEquipped { get; set; } = false;
    public bool IsStackable { get; set; } = true;
    public int? EquippedSlot { get; set; }
    public string SizeLabel { get; set; } = string.Empty;   // "Petty" / "Bulky" / empty for Regular
    public bool HasSizeLabel => !string.IsNullOrEmpty(SizeLabel);

    private string? _playerNote;

    public string EquipmentState
    {
        get
        {
            if (!IsEquipped) return string.Empty;

            if (EquippedSlotType == "Armor")
                return "🛡️ Current Armor";
            else if (EquippedSlotType == "Belt")
                return "🔗 Belted";
            else if (EquippedSlotType == "Hand")
                return "🔒 Equipped";

            return "🔒 Equipped";
        }
    }

    public string EquippedSlotType { get; set; } = string.Empty;

    // ===== ITEM METADATA FOR PLAYER NOTES =====
    public string? PlayerNote
    {
        get => _playerNote;
        set
        {
            _playerNote = value;
            OnPropertyChanged();
        }
    }

    public ICommand? EditNoteCommand { get; set; }
    // ===== ITEM METADATA FOR USES =====
    public int? MaxUses { get; set; }

    private int _remainingUses;
    public int RemainingUses
    {
        get => _remainingUses;
        set
        {
            if (_remainingUses == value) return;
            _remainingUses = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UsesText));
            OnPropertyChanged(nameof(UsesFillRatio));
        }
    }

    // Tile uses readout: "2/3" and a 0-1 fill for the bar
    public string UsesText => HasUses ? $"{RemainingUses}/{MaxUses}" : string.Empty;
    public double UsesFillRatio => HasUses ? Math.Clamp((double)RemainingUses / MaxUses!.Value, 0, 1) : 0;

    // ===== RARITY (tile border: 1 lime green, 2 purple, 3 gold) =====
    private static readonly Brush LimeBrush = new SolidColorBrush(Color.FromArgb("#32CD32"));
    private static readonly Brush PurpleBrush = new SolidColorBrush(Color.FromArgb("#A020F0"));
    private static readonly Brush GoldBrush = new SolidColorBrush(Color.FromArgb("#FFD700"));

    public int? Rarity { get; set; }
    public Brush RarityBrush => Rarity switch { 1 => LimeBrush, 2 => PurpleBrush, 3 => GoldBrush, _ => Brush.Transparent };
    public double RarityThickness => Rarity is >= 1 and <= 3 ? 2 : 0;

    public string UsesDisplay
    {
        get
        {
            // Unlimited items show nothing
            if (MaxUses == -1 || MaxUses == null) return string.Empty;
            if (MaxUses > 0)
                return $"Uses: {RemainingUses}/{MaxUses}";
            return string.Empty;
        }
    }

    public bool HasUses => MaxUses.HasValue && MaxUses.Value > 0;
    public bool IsUnlimited => !MaxUses.HasValue || MaxUses.Value == -1;

    public bool IsDropMode
    {
        get => _isDropMode;
        set
        {
            _isDropMode = value;
            OnPropertyChanged();
        }
    }

    public ICommand? DropCommand { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}