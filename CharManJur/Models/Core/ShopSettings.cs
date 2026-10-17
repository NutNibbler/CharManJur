namespace CharManJur.Models;

// The shop's rules for the current session. Runtime only, never saved: single-player keeps the
// defaults (open, every category, no inflation); a GM host will set these once multiplayer exists.
public static class ShopSettings
{
    public static event Action? Changed;

    private static bool _isOpen = true;
    private static IReadOnlySet<ItemCategory>? _allowedCategories;   // null = every category
    private static decimal _purchaseInflation;                        // 0 = none, 0.2 = +20%
    private static decimal _saleInflation;                            // same, applied to sale prices

    public static bool IsOpen
    {
        get => _isOpen;
        set { if (_isOpen == value) return; _isOpen = value; Changed?.Invoke(); }
    }

    public static IReadOnlySet<ItemCategory>? AllowedCategories
    {
        get => _allowedCategories;
        set { _allowedCategories = value; Changed?.Invoke(); }
    }

    public static decimal PurchaseInflation
    {
        get => _purchaseInflation;
        set { if (_purchaseInflation == value) return; _purchaseInflation = value; Changed?.Invoke(); }
    }

    public static decimal SaleInflation
    {
        get => _saleInflation;
        set { if (_saleInflation == value) return; _saleInflation = value; Changed?.Invoke(); }
    }

    public static void Reset()
    {
        _isOpen = true;
        _allowedCategories = null;
        _purchaseInflation = 0m;
        _saleInflation = 0m;
        Changed?.Invoke();
    }
}