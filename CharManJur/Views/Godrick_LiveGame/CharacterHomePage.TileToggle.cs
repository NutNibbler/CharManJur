using CharManJur.Models;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CharacterHomePage
{
    // Set whenever an item becomes selected (see OnViewModelPropertyChanged).
    private long _selectionChangedAt;

    // Kept so the existing call in InitializePanels still compiles. The selection time is now tracked in
    // OnViewModelPropertyChanged. You can delete this method and that call.
    private void InitializeTileToggle() { }

    // The tile's one tap handler. A tap on the item that is already selected hides the details pane.
    // OnDropItemTapped selects the tapped item on every tap, so the state is read BEFORE it runs.
    private void OnInventoryTileTapped(object? sender, TappedEventArgs e)
    {
        bool dropMode = _viewModel.IsDropModeActive;

        var tapped = (sender as BindableObject)?.BindingContext as InventoryItemDisplay;
        var before = _viewModel.SelectedInventoryItem;
        bool alreadySelected = tapped != null && before != null && before.Id == tapped.Id;

        // The list can select the item just before this tap arrives; that selection isn't a "second tap".
        bool justSelected = Environment.TickCount64 - _selectionChangedAt < 400;

        OnDropItemTapped(sender!, e);   // Drop Mode: drops the item. Otherwise: selects the tapped item.

        if (dropMode || tapped == null) return;
        if (alreadySelected && !justSelected)
            _viewModel.SelectedInventoryItem = null;   // second tap: hide the details pane
    }
}