using CharManJur.Models;

namespace CharManJur.Services;

// Stand-in until multiplayer / GM hosting exists.
public class LocalOnlyItemTransferGateway : IItemTransferGateway
{
    public bool IsAvailable => false;

    public string UnavailableReason =>
        "Transfers to other players need a hosted session, which isn't available yet.";

    public Task<IReadOnlyList<TransferRecipient>> GetRecipientsAsync() =>
        Task.FromResult<IReadOnlyList<TransferRecipient>>(Array.Empty<TransferRecipient>());

    public Task<TransferResult> SendAsync(ItemTransferPacket packet) =>
        Task.FromResult(new TransferResult(false, UnavailableReason));
}