using CharManJur.Models;
using CharManJur.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// The Transfer button's flow: pick a destination (a familiar or another player's character), then move the item.
public static class ItemTransferHelper
{
    private const string PlayerOption = "👤 Another player's character…";

    // Returns null when cancelled.
    public static async Task<int?> PromptQuantityAsync(CharacterItem item, string title, string accept)
    {
        if (item.Quantity <= 1) return 1;

        string? answer = await Application.Current!.MainPage!.DisplayPromptAsync(
            title, $"How many {item.DisplayName} (1–{item.Quantity})?",
            accept, "Cancel", initialValue: item.Quantity.ToString(), keyboard: Keyboard.Numeric);

        if (answer == null || !int.TryParse(answer, out int quantity) || quantity < 1) return null;
        return Math.Min(quantity, item.Quantity);
    }

    // Returns true when the item left this character's pack.
    public static async Task<bool> TransferAsync(CharacterItem item, ICharAttribDataService svc)
    {
        var page = Application.Current!.MainPage!;

        if (item.IsEquipped)
        {
            await page.DisplayAlertAsync("Unequip First", "Unequip this item before transferring it.", "OK");
            return false;
        }
        if (svc.IsPackDropped)
        {
            await page.DisplayAlertAsync("Pack Dropped", "Pick up your pack before transferring items.", "OK");
            return false;
        }

        var familiars = svc.AcquiredFamiliars.Where(f => !f.IsDead).ToList();
        var labels = familiars.Select((f, i) => $"🐾 {i + 1}. {f.DisplayName}").ToList();
        labels.Add(PlayerOption);

        string? picked = await page.DisplayActionSheetAsync(
            $"Transfer {item.DisplayName} to…", "Cancel", null, labels.ToArray());
        if (picked == null || picked == "Cancel") return false;

        if (picked == PlayerOption) return await TransferToPlayerAsync(item, svc);

        int index = labels.IndexOf(picked);
        if (index < 0 || index >= familiars.Count) return false;
        return await FamiliarHandoffHelper.HandToFamiliarAsync(item, svc, familiars[index]);
    }

    private static async Task<bool> TransferToPlayerAsync(CharacterItem item, ICharAttribDataService svc)
    {
        var page = Application.Current!.MainPage!;
        var gateway = IPlatformApplication.Current!.Services.GetRequiredService<IItemTransferGateway>();

        if (!gateway.IsAvailable)
        {
            await page.DisplayAlertAsync("Not Available Yet", gateway.UnavailableReason, "OK");
            return false;
        }

        var recipients = await gateway.GetRecipientsAsync();
        if (recipients.Count == 0)
        {
            await page.DisplayAlertAsync("No One to Send To", "No other player characters are in this session.", "OK");
            return false;
        }

        var labels = recipients.Select(r => r.DisplayName).ToArray();
        string? picked = await page.DisplayActionSheetAsync("Send to which character?", "Cancel", null, labels);
        int index = picked == null ? -1 : Array.IndexOf(labels, picked);
        if (index < 0) return false;

        int? quantity = await PromptQuantityAsync(item, "Send Item", "Send");
        if (quantity == null) return false;

        var packet = svc.BuildTransferPacket(item.Id, quantity.Value, recipients[index]);
        if (packet == null) return false;

        var result = await gateway.SendAsync(packet);
        if (!result.Success)
        {
            await page.DisplayAlertAsync("Transfer Failed", result.Message ?? "The item could not be sent.", "OK");
            return false;
        }

        svc.CompleteTransferOut(item.Id, packet);   // only now does the item leave this pack
        return true;
    }
}