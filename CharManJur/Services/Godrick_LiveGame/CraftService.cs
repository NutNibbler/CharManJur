using CharManJur.Models;

namespace CharManJur.Services;

// How many of one pack record the player committed to a craft.
public sealed record CraftCommit(int CharacterItemId, int Quantity);

// One of the character's items that can be committed to a craft.
public sealed class CraftInput
{
    public int CharacterItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Carried { get; init; }
    public bool IsMaterial { get; init; }          // Resource category
    public string CategoryText { get; init; } = string.Empty;
}

// Crafting: commit any of your items (always consumed) to make an item, or a construct familiar.
// A blueprint only changes how it is logged and what hint is shown; its material cost is a recommendation.
public class CraftService
{
    public const int MaxOutputQuantity = 999;

    private readonly ICharAttribDataService _svc;
    private readonly IItemDataService _items;
    private readonly IPlayerActionLogService _log;
    // The item data service, so the craft screen can open the item creator.
    public IItemDataService ItemData => _items;

    private sealed record Consumed(CharacterItem Item, int Quantity, string Name);

    public CraftService(ICharAttribDataService svc, IItemDataService items, IPlayerActionLogService log)
    {
        _svc = svc;
        _items = items;
        _log = log;
    }

    // Everything that can be crafted: every loaded item except money.
    public async Task<List<Item>> OutputCatalogAsync()
    {
        var all = await _items.GetAllItemsAsync();
        return all.Where(i => i.Category != ItemCategory.Currency)
                  .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                  .ToList();
    }

    // What the character can commit: anything carried and not equipped, except money.
    public List<CraftInput> Inputs()
        => _svc.Inventory
            .Where(i => !i.IsDropped && !i.IsEquipped && i.Template != null && !CurrencyHelper.IsCurrency(i))
            .OrderByDescending(i => i.Template!.Category == ItemCategory.Resource)
            .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(i => new CraftInput
            {
                CharacterItemId = i.Id,
                Name = i.DisplayName,
                Carried = i.Quantity,
                IsMaterial = i.Template!.Category == ItemCategory.Resource,
                CategoryText = i.Template.Category.HasValue ? EnumDisplayHelper.GetName(i.Template.Category.Value) : "Other"
            })
            .ToList();

    // ===== ITEM OUTPUT =====
    // startingUses: how many uses the crafted item starts with (null = full).
    public async Task<SubFeatureResult> CraftItemAsync(
        Item output, int quantity, IReadOnlyList<CraftCommit> committed, SubFeatureEntry? blueprint, int? startingUses = null)
    {
        if (output == null || quantity < 1 || quantity > MaxOutputQuantity) return Fail("Choose what to craft.");

        var used = Check(committed, out string error);
        if (used == null) return Fail(error);

        Consume(used);
        _svc.AddItemToInventory(output, quantity, null, startingUses);
        _svc.UpdateEncumbranceState();

        await LogAsync(
            blueprint != null ? PlayerActionType.BlueprintCraftedItem : PlayerActionType.SandboxCraftedItem,
            output.Name, output.Id,
            $"Crafted {quantity}x {output.Name} {How(blueprint)} — consumed: {ConsumedText(used)}{MaterialsNote(used, blueprint)}{UsesNote(output, quantity, startingUses)}");

        await _svc.SaveNowAsync();
        return Ok();
    }

    private static string UsesNote(Item output, int quantity, int? startingUses)
        => startingUses.HasValue && output.HasUses && !(output.IsStackableItem && quantity > 1)
           && startingUses.Value < output.Uses!.Value
            ? $" [starting uses {startingUses}/{output.Uses}]"
            : string.Empty;

    // ===== CONSTRUCT OUTPUT =====
    // The construct belongs to this character only: a self-contained familiar instance (its own InstanceId,
    // every field embedded), so it saves with the character and needs no template lookup anywhere.
    public async Task<SubFeatureResult> CraftConstructAsync(
        string name, string description, FmlrSizes size, int capacity,
        IReadOnlyList<CraftCommit> committed, SubFeatureEntry? blueprint)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0) return Fail("Name your construct.");
        if (capacity < 0) return Fail("Base capacity can't be negative.");

        var used = Check(committed, out string error);
        if (used == null) return Fail(error);

        Consume(used);

        var construct = new Familiar
        {
            Guid = Guid.NewGuid(),                    // stable identity; not part of any template list
            Id = 0,
            IsLoaded = true,
            SourcePackId = "Crafted",
            LastModified = DateTime.UtcNow,
            FmlrName = name,
            FmlrDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            FmlrClass = FmlrClasses.Construct,
            FmlrSize = size,
            FmlrIntelligence = FmlrIntelligences.Inanimate,
            // Stats exist only so the shared familiar code works; the panel hides them for Inanimate familiars.
            HP = 1,
            StatVigor = 10,
            StatAgility = 10,
            StatMind = 10,
            StatSpirit = 10,
            IsPlayerCreated = true,
            IsFoundation = false,
            BaseInventoryCapacity = capacity,
            InstanceId = Guid.NewGuid()
        };
        _svc.AcquiredFamiliars.Add(construct);

        await LogAsync(
            PlayerActionType.ConstructCrafted, name, null,
            $"Crafted construct \"{name}\" ({EnumDisplayHelper.GetName(size)}, base capacity {capacity}) " +
            $"{How(blueprint)} — consumed: {ConsumedText(used)}{MaterialsNote(used, blueprint)}");

        await _svc.SaveNowAsync();
        return Ok();
    }

    // ===== SHARED =====
    // Checks everything first, so a failure changes nothing.
    private List<Consumed>? Check(IReadOnlyList<CraftCommit> committed, out string error)
    {
        error = string.Empty;
        if (_svc.IsPackDropped) { error = "Pick up your pack before crafting."; return null; }
        if (committed.Count == 0 || committed.Any(c => c.Quantity < 1)) { error = "Commit at least one item to craft with."; return null; }
        if (committed.GroupBy(c => c.CharacterItemId).Any(g => g.Count() > 1)) { error = "An item was committed twice."; return null; }

        var used = new List<Consumed>();
        foreach (var c in committed)
        {
            var item = _svc.Inventory.FirstOrDefault(i => i.Id == c.CharacterItemId);
            if (item?.Template == null || item.IsDropped) { error = "One of the items you chose is no longer in your pack."; return null; }
            if (item.IsEquipped) { error = $"Unequip {item.DisplayName} before using it to craft."; return null; }
            if (c.Quantity > item.Quantity) { error = $"You don't have that many {item.DisplayName}."; return null; }
            used.Add(new Consumed(item, c.Quantity, item.DisplayName));
        }
        return used;
    }

    private void Consume(List<Consumed> used)
    {
        foreach (var u in used)
        {
            if (u.Quantity >= u.Item.Quantity)
            {
                _svc.RemoveItemFromInventory(u.Item.Id);
            }
            else
            {
                u.Item.Quantity -= u.Quantity;
                u.Item.LastModified = DateTime.Now;
            }
        }
    }

    private static string ConsumedText(List<Consumed> used) => string.Join(", ", used.Select(u => $"{u.Quantity}x {u.Name}"));

    private static string How(SubFeatureEntry? blueprint) => blueprint != null ? $"with blueprint {blueprint.Name}" : "by sandbox crafting";

    private static string MaterialsNote(List<Consumed> used, SubFeatureEntry? blueprint)
    {
        if (blueprint?.Source is not Blueprint bp) return string.Empty;
        int materials = used.Where(u => u.Item.Template!.Category == ItemCategory.Resource).Sum(u => u.Quantity);
        return $" [materials: {materials} of {bp.BlueprintCost} recommended]";
    }

    private Task LogAsync(PlayerActionType type, string itemName, int? itemId, string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            ItemName = itemName,
            ItemId = itemId,
            Summary = summary
        });

    private static SubFeatureResult Ok() => new() { Success = true };
    private static SubFeatureResult Fail(string message) => new() { Success = false, Message = message };
}