using CharManJur.Models;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

// Each familiar in the equipment column is a drop target: dragging a pack item onto it hands the item over.
public partial class CharacterHomePage
{
    // Wired from XAML (HandlerChanged) on each familiar tile, the same way the pack tiles are.
    private void OnFamiliarStripHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is not Border tile || tile.Handler == null) return;
        if (tile.GestureRecognizers.OfType<DropGestureRecognizer>().Any()) return;

        var drop = new DropGestureRecognizer { AllowDrop = true };

        drop.DragOver += (s, args) =>
        {
            // Always accept, so a refused drop can explain itself; the glow shows validity.
            args.AcceptedOperation = DataPackageOperation.Copy;
            var item = GetDraggedItem();
            var familiar = (tile.BindingContext as FamiliarStripItem)?.Familiar;
            bool valid = item != null && familiar != null && CanHandTo(item, familiar);
            SetDropHighlight(tile, valid ? Colors.Gold : Colors.IndianRed);
        };

        drop.DragLeave += (s, args) => SetDropHighlight(tile, null);

        drop.Drop += async (s, args) =>
        {
            SetDropHighlight(tile, null);

            var item = GetDraggedItem();
            var strip = tile.BindingContext as FamiliarStripItem;
            _draggedItemId = null;
            if (item == null || strip == null) return;

            await HandToFamiliarByDropAsync(item, strip.Familiar);
        };

        tile.GestureRecognizers.Add(drop);
    }

    private bool CanHandTo(CharacterItem item, Familiar familiar)
        => !familiar.IsDead && !item.IsEquipped && !_viewModel.IsPackDropped;

    private async Task HandToFamiliarByDropAsync(CharacterItem item, Familiar familiar)
    {
        if (familiar.IsDead)
        {
            await DisplayAlertAsync("Can't Hand Over", $"{familiar.DisplayName} is dead and can't receive items.", "OK");
            return;
        }

        int? selectedId = _viewModel.SelectedInventoryItem?.Id;

        // The helper checks equipped / dropped pack, asks how many (default all) and logs the hand-over.
        if (!await FamiliarHandoffHelper.HandToFamiliarAsync(item, _viewModel.CharDataService, familiar)) return;

        await RefreshAfterDropAsync();

        // Keep the details pane on the same item if it's still in the pack; close it if the item is gone.
        if (selectedId.HasValue)
            _viewModel.SelectedInventoryItem = _viewModel.InventoryItems.FirstOrDefault(i => i.Id == selectedId.Value);
    }
}