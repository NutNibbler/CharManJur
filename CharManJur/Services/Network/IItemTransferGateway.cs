using CharManJur.Models;

namespace CharManJur.Services;

public record TransferResult(bool Success, string? Message = null);

// The seam between item transfers and whatever carries them to another player.
// Today: local only. Later: a hosted-session implementation.
public interface IItemTransferGateway
{
    bool IsAvailable { get; }
    string UnavailableReason { get; }
    Task<IReadOnlyList<TransferRecipient>> GetRecipientsAsync();
    Task<TransferResult> SendAsync(ItemTransferPacket packet);
}