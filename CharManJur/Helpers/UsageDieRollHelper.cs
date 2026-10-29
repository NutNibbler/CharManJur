using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Helpers;

public static class UsageDieRollHelper
{
    // Rolls one item's usage die on demand. Returns null when cancelled or the item has no die.
    public static async Task<UsageDieOutcome?> RollAsync(Page page, ICharAttribDataService svc, CharacterItem item)
    {
        if (item.Template?.IsHybridUsage == true && item.IsEmpty)
        {
            await page.DisplayAlertAsync("Empty", $"{item.DisplayName} has no uses left. Refill it before rolling.", "OK");
            return null;
        }

        if (!item.HasUsageDie) return null;

        int sides = item.UsageDieSides;
        string die = UsageDie.Label(sides);

        string? mode = await page.DisplayActionSheetAsync(
            $"Roll {item.DisplayName} ({die})", "Cancel", null, "Roll for Me", "Enter Result");

        int roll;
        if (mode == "Roll for Me")
        {
            roll = Random.Shared.Next(1, sides + 1);
        }
        else if (mode == "Enter Result")
        {
            while (true)
            {
                string? text = await page.DisplayPromptAsync(
                    "Usage Die", $"Result of your {die} (1 to {sides}):", "OK", "Cancel", keyboard: Keyboard.Numeric);
                if (text == null) return null;
                if (int.TryParse(text, out roll) && roll >= 1 && roll <= sides) break;
                await page.DisplayAlertAsync("Invalid Roll", $"Enter a number from 1 to {sides}.", "OK");
            }
        }
        else
        {
            return null;
        }

        var outcome = svc.ResolveUsageDieRoll(item.Id, roll);
        if (outcome != null)
            await page.DisplayAlertAsync("Usage Die", outcome.Summary, "OK");
        return outcome;
    }
}