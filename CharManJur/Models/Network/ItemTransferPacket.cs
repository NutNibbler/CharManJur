using System;

namespace CharManJur.Models;

// What travels between players when an item is transferred.
// Plain data so it can be serialized straight onto whatever transport multiplayer ends up using.
public class ItemTransferPacket
{
    public Guid TransferId { get; set; } = Guid.NewGuid();
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    // Sender
    public Guid FromPlayerId { get; set; }
    public string FromPlayerName { get; set; } = string.Empty;
    public Guid FromCharacterGuid { get; set; }
    public string FromCharacterName { get; set; } = string.Empty;

    // Recipient
    public Guid ToCharacterGuid { get; set; }
    public string ToCharacterName { get; set; } = string.Empty;
    public string ToPlayerName { get; set; } = string.Empty;

    // Item payload (the receiver looks the template up from these)
    public int TemplateId { get; set; }
    public Guid? TemplateGuid { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid ItemInstanceId { get; set; }
    public int Quantity { get; set; } = 1;
    public int RemainingUses { get; set; }
    public int UsageDieSides { get; set; }
    public string? CustomName { get; set; }
    public string? PlayerNote { get; set; }
    public int? WrittenValueInChips { get; set; }
    public bool IsIdentified { get; set; } = true;
}

// Another player's character that items can be sent to.
public class TransferRecipient
{
    public Guid CharacterGuid { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(PlayerName)
        ? CharacterName
        : $"{CharacterName} ({PlayerName})";
}