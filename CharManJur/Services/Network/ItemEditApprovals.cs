using CharManJur.Models;

namespace CharManJur.Services;

// A player's proposed change to an item definition that somebody already owns.
public sealed record ItemEditRequest(
    Guid RequestId,
    Guid ItemGuid,
    int ItemId,
    string ItemName,
    Item Proposed,
    IReadOnlyList<string> Changes,
    string RequestedBy,
    DateTime RequestedUtc);

// The GM's approvals tray. Until the GM approves, the item itself is never changed.
public static class ItemEditApprovals
{
    private static readonly List<ItemEditRequest> _pending = new();

    public static IReadOnlyList<ItemEditRequest> Pending => _pending;
    public static event EventHandler? Changed;

    public static void Submit(ItemEditRequest request)
    {
        // A newer request for the same item from the same player replaces the older one
        _pending.RemoveAll(r => r.ItemGuid == request.ItemGuid && r.RequestedBy == request.RequestedBy);
        _pending.Add(request);
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void Discard(Guid requestId)
    {
        if (_pending.RemoveAll(r => r.RequestId == requestId) > 0)
            Changed?.Invoke(null, EventArgs.Empty);
    }
}