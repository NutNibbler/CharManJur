using CharManJur.Models;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

// Drag-and-drop for the home page: pack tiles, hand slots, belt slots, armor, and the inventory panel.
public partial class CharacterHomePage
{
    private int? _draggedItemId;

    // Called once from the constructor: wires up the targets defined in XAML.
    private void InitializeDragAndDrop()
    {
        AttachDragSource(ArmorBorder, () => _viewModel.CharDataService.GetEquippedArmor());
        AttachDropTarget(ArmorBorder,
            item => EquipmentActionHelper.CanDropOnArmor(item, _viewModel.CharDataService),
            item => EquipmentActionHelper.DropOnArmorAsync(item, _viewModel.CharDataService));

        // Dropping an equipped item on the inventory panel returns it to the pack
        AttachDropTarget(InventoryBorder,
            item => EquipmentActionHelper.CanDropOnPack(item, _viewModel.CharDataService),
            item => EquipmentActionHelper.DropOnPackAsync(item, _viewModel.CharDataService));
    }

    // Used by GenerateLimbSetDisplay for each hand cell (or the merged two-handed cell).
    private void AttachHandSlotDragDrop(Border border, CharacterHomeViewModel.LimbSlotDisplay slot)
    {
        if (slot.IsOccupied)
        {
            AttachDragSource(border, () => slot.OccupiedByItem);
        }

        AttachDropTarget(border,
            item => EquipmentActionHelper.CanDropOnHandSlot(item, _viewModel.CharDataService),
            item => EquipmentActionHelper.DropOnHandSlotAsync(item, slot.SlotNumber, _viewModel.CharDataService));
    }

    // Used by GenerateBeltSlotDisplay for each belt cell (or the merged bulky cell).
    private void AttachBeltSlotDragDrop(Border border, CharacterHomeViewModel.BeltSlotDisplay slot)
    {
        if (slot.IsOccupied)
        {
            AttachDragSource(border, () => slot.OccupiedByItem);
        }

        AttachDropTarget(border,
            item => EquipmentActionHelper.CanDropOnBeltSlot(item, _viewModel.CharDataService),
            item => EquipmentActionHelper.DropOnBeltSlotAsync(item, slot.SlotNumber, _viewModel.CharDataService));
    }

    // ===== Pack tiles (hooked up in XAML) =====
    private void OnPackTileDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (_viewModel.IsDropModeActive)
        {
            e.Cancel = true;
            return;
        }

        var display = (sender as BindableObject)?.BindingContext as InventoryItemDisplay;
        var item = display == null
            ? null
            : _viewModel.CharDataService.Inventory.FirstOrDefault(i => i.Id == display.Id);

        BeginDrag(e, item);
    }

    private void OnPackTileDropCompleted(object? sender, DropCompletedEventArgs e) => _draggedItemId = null;

    // ===== Pack tile drop target (hooked up in XAML via HandlerChanged) =====
    // Dropping a pack tile onto another tile of the same item merges the stacks, or swaps them
    // when one is equipped. The recognizer is added in code so its handlers reach the tile
    // directly; CollectionView recycles tiles, so the item is read at drop time.
    private void OnPackTileHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is not Border tile || tile.Handler == null) return;
        if (tile.GestureRecognizers.OfType<DropGestureRecognizer>().Any()) return;

        var drop = new DropGestureRecognizer { AllowDrop = true };

        drop.DragOver += (s, args) =>
        {
            args.AcceptedOperation = DataPackageOperation.Copy;
            var dragged = GetDraggedItem();
            var target = GetTileItem(tile);
            bool match = dragged != null && target != null && IsSameItemPair(dragged, target);
            SetDropHighlight(tile, match ? Colors.Gold : null);
        };

        drop.DragLeave += (s, args) => SetDropHighlight(tile, null);

        drop.Drop += async (s, args) =>
        {
            SetDropHighlight(tile, null);

            var dragged = GetDraggedItem();
            var target = GetTileItem(tile);
            _draggedItemId = null;
            if (dragged == null || target == null || dragged.Id == target.Id) return;

            var svc = _viewModel.CharDataService;

            if (!IsSameItemPair(dragged, target))
            {
                // Not a merge: an equipped item dropped anywhere on the inventory still returns to the pack.
                if (EquipmentActionHelper.CanDropOnPack(dragged, svc)
                    && await EquipmentActionHelper.DropOnPackAsync(dragged, svc))
                {
                    await RefreshAfterDropAsync();
                }
                return;
            }

            switch (svc.MergeItemStacks(dragged.Id, target.Id))
            {
                case StackMergeResult.Merged:
                case StackMergeResult.Swapped:
                    await RefreshAfterDropAsync();
                    break;
                case StackMergeResult.NotStackable:
                    await DisplayAlertAsync("Can't Merge", "Only stackable items can be merged.", "OK");
                    break;
                case StackMergeResult.BothOpened:
                    await DisplayAlertAsync("Can't Merge", "Both stacks have been opened, so they can't be merged.", "OK");
                    break;
                case StackMergeResult.TargetFull:
                    await DisplayAlertAsync("Can't Merge", "That stack is already full.", "OK");
                    break;
                case StackMergeResult.EmptyItem:
                    await DisplayAlertAsync("Can't Merge", "Empty items can't be merged. It should be dropped.", "OK");
                    break;
                default:
                    await DisplayAlertAsync("Can't Merge", "Those items can't be merged or swapped right now.", "OK");
                    break;
            }
        };

        tile.GestureRecognizers.Add(drop);
    }

    private CharacterItem? GetTileItem(Border tile)
        => tile.BindingContext is InventoryItemDisplay display
            ? _viewModel.CharDataService.Inventory.FirstOrDefault(i => i.Id == display.Id)
            : null;

    private static bool IsSameItemPair(CharacterItem a, CharacterItem b)
        => a.Id != b.Id && a.Template != null && b.Template != null && ItemRecordTransfer.SameItem(a, b);

    // ===== Shared plumbing =====
    private void BeginDrag(DragStartingEventArgs e, CharacterItem? item)
    {
        // Items sitting in a dropped pack can't be picked up; jewelry has nowhere to go.
        if (item == null
            || item.IsJewelry
            || (_viewModel.IsPackDropped && !item.IsEquipped))
        {
            e.Cancel = true;
            return;
        }

        _draggedItemId = item.Id;
        e.Data.Text = item.DisplayName;
        e.Data.Properties["itemId"] = item.Id;
    }

    private CharacterItem? GetDraggedItem()
        => _draggedItemId.HasValue
            ? _viewModel.CharDataService.Inventory.FirstOrDefault(i => i.Id == _draggedItemId.Value)
            : null;

    private void AttachDragSource(View border, Func<CharacterItem?> getItem)
    {
        var drag = new DragGestureRecognizer { CanDrag = true };
        drag.DragStarting += (s, e) => BeginDrag(e, getItem());
        drag.DropCompleted += (s, e) => _draggedItemId = null;
        border.GestureRecognizers.Add(drag);
    }

    private void AttachDropTarget(
        View border,
        Func<CharacterItem, bool> isValid,
        Func<CharacterItem, Task<bool>> execute)
    {
        var drop = new DropGestureRecognizer { AllowDrop = true };

        drop.DragOver += (s, e) =>
        {
            // Always accept, so a refused drop can explain itself; the glow shows validity.
            e.AcceptedOperation = DataPackageOperation.Copy;
            var item = GetDraggedItem();
            bool valid = item != null && isValid(item);
            SetDropHighlight(border, valid ? Colors.Gold : Colors.IndianRed);
        };

        drop.DragLeave += (s, e) => SetDropHighlight(border, null);

        drop.Drop += async (s, e) =>
        {
            SetDropHighlight(border, null);

            var item = GetDraggedItem();
            _draggedItemId = null;
            if (item == null) return;

            if (await execute(item))
            {
                await RefreshAfterDropAsync();
            }
        };

        border.GestureRecognizers.Add(drop);
    }

    private static void SetDropHighlight(View border, Color? color)
    {
        border.Shadow = color == null
            ? null
            : new Shadow
            {
                Brush = color,
                Radius = 12,
                Opacity = 0.9f,
                Offset = new Point(0, 0)
            };
    }

    private async Task RefreshAfterDropAsync()
    {
        await _viewModel.LoadCharacterDataAsync();
        _viewModel.UpdateLimbSetDisplays();
        GenerateLimbSetDisplay();
        GenerateBeltSlotDisplay();
    }
}