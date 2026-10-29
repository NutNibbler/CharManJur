using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

// Apply is null when the bestow can't be done; Error then says why. Apply returns the text for the GM.
public sealed record BestowPlan(Func<string>? Apply, string Error);

// Turns what the GM's bestow page did into steps on the player's real character. Everything is checked first,
// so a bestow that can't be done is refused as a whole instead of half arriving.
public static class PlayerBestow
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task<BestowPlan> PrepareAsync(
        List<RemoteBestowOp>? ops, ICharAttribDataService svc, IItemDataService items, IPlayerActionLogService log)
    {
        if (ops == null || ops.Count == 0 || ops.Count > 100)
            return Fail("There was nothing to bestow.");

        var steps = new List<Func<string?>>();   // each returns a short note when it could not be done, or null

        foreach (var op in ops)
        {
            var current = op;

            switch (current.Op)
            {
                case "item":
                    {
                        Item? template = current.ItemGuid != Guid.Empty
                            ? await items.GetItemByGuidAsync(current.ItemGuid)
                            : await items.GetItemByIdAsync(current.ItemId);
                        if (template == null)
                            return Fail($"{current.Label} isn't in this player's asset packs yet.");

                        int quantity = Math.Clamp(current.Quantity, 1, 9999);

                        if (current.FamiliarInstanceId != Guid.Empty)
                        {
                            var familiar = svc.GetFamiliar(current.FamiliarInstanceId);
                            if (familiar == null || familiar.IsDead)
                                return Fail($"{current.Label}: that familiar is no longer available.");

                            steps.Add(() => svc.AddItemToFamiliarInventory(
                                current.FamiliarInstanceId, template, quantity, current.WrittenValue, current.StartingUses, current.UsageDie)
                                ? null : current.Label);
                        }
                        else
                        {
                            steps.Add(() =>
                            {
                                var added = svc.AddItemToInventory(template, quantity, current.WrittenValue, current.StartingUses, current.UsageDie);
                                if (added == null) return current.Label;

                                _ = log.LogItemBestowedAsync(added, svc.PlayerName, svc.PlayerId, svc.CharacterName, svc.CharacterGuid);
                                return null;
                            });
                        }
                        break;
                    }

                case "familiar":
                    {
                        Familiar? template = string.IsNullOrEmpty(current.FamiliarJson)
                            ? null
                            : JsonSerializer.Deserialize<Familiar>(current.FamiliarJson, Json);
                        if (template == null)
                            return Fail($"{current.Label} could not be read.");

                        steps.Add(() =>
                        {
                            var instance = svc.BestowFamiliar(template);
                            GameNotices.Post(new GameNotice("🐾 Familiar bestowed", instance.DisplayName, NoticeSource.Player, 6));
                            return null;
                        });
                        break;
                    }

                default:
                    return Fail($"This version of the app doesn't know the bestow step '{current.Op}'.");
            }
        }

        return new BestowPlan(() =>
        {
            var failed = new List<string>();
            foreach (var step in steps)
            {
                var problem = step();
                if (problem != null) failed.Add(problem);
            }

            return failed.Count == 0
                ? "Delivered."
                : "Some of it couldn't be given: " + string.Join(", ", failed);
        }, string.Empty);
    }

    private static BestowPlan Fail(string why) => new(null, why);
}