using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public partial class CharacterHomeViewModel
{
    public void ShowToast(string title, string message, double seconds = 4)
        => GameNotices.Post(new GameNotice(title, message, NoticeSource.Player, seconds));

    // ===== ITEM BESTOWMENT =====
    // The Bestow page reports back after items arrive. Comparing the pack before and after tells the toast what came in.
    private Dictionary<string, (string Name, int Quantity)> InventoryTotals()
    {
        var totals = new Dictionary<string, (string Name, int Quantity)>();
        foreach (var record in _charDataService.Inventory.Where(i => !i.IsDropped && i.Template != null))
        {
            string key = record.TemplateGuid.HasValue ? $"g:{record.TemplateGuid}" : $"i:{record.TemplateId}";
            totals[key] = totals.TryGetValue(key, out var t)
                ? (t.Name, t.Quantity + record.Quantity)
                : (record.Template!.Name, record.Quantity);
        }
        return totals;
    }

    private static int QuantityOf(Dictionary<string, (string Name, int Quantity)> totals, string key)
        => totals.TryGetValue(key, out var t) ? t.Quantity : 0;

    private void AnnounceBestowed(Dictionary<string, (string Name, int Quantity)> before)
    {
        var after = InventoryTotals();

        var gained = after
            .Select(a => (a.Value.Name, Delta: a.Value.Quantity - QuantityOf(before, a.Key)))
            .Where(g => g.Delta > 0)
            .ToList();

        before.Clear();
        foreach (var kv in after) before[kv.Key] = kv.Value;

        if (gained.Count == 0) return;

        int total = gained.Sum(g => g.Delta);
        var shown = gained.Take(5).Select(g => $"{g.Delta}× {g.Name}").ToList();
        string message = string.Join(", ", shown) + (gained.Count > 5 ? $" and {gained.Count - 5} more" : "");

        GameNotices.Post(new GameNotice(total == 1 ? "📦 Item bestowed" : $"📦 {total} items bestowed",
            message, NoticeSource.Player, 6));
    }


    // The GM bestowed something over the network: do it, then refresh the screen and say what arrived, exactly
    // like the Bestow page does when the player bestows to themselves.
    public async Task ApplyGmBestowAsync(Action apply)
    {
        var before = InventoryTotals();
        apply();
        await LoadCharacterDataAsync();
        AnnounceBestowed(before);
    }
}