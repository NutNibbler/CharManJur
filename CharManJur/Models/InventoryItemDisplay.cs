using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.Models;

public class InventoryItemDisplay : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isDestroyMode;

    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public bool IsEmpty { get; set; } = false;
    public bool IsEquipped { get; set; } = false;
    public bool IsStackable { get; set; } = true;
    public int? EquippedSlot { get; set; }

    private string? _playerNote;

    public string EquipmentState
    {
        get
        {
            if (!IsEquipped) return string.Empty;

            if (EquippedSlotType == "Armor")
                return "🛡️ Current Armor";
            else if (EquippedSlotType == "Belt")
                return "🪢 Belted";
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
    public int RemainingUses { get; set; } = 0;

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

    public bool IsDestroyMode
    {
        get => _isDestroyMode;
        set
        {
            _isDestroyMode = value;
            OnPropertyChanged();
        }
    }

    public ICommand? DestroyCommand { get; set; }

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