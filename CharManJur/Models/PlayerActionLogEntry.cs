using System;

namespace CharManJur.Models;

public enum PlayerActionType
{
    ItemDropped,
    ItemBestowed
}

public class PlayerActionLogEntry
{
    public PlayerActionType ActionType { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public Guid PlayerId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int ItemId { get; set; }   // TemplateId, per earlier decision
    public DateTime Timestamp { get; set; } = DateTime.Now;

    private string ActionTypeDisplay => ActionType switch
    {
        PlayerActionType.ItemDropped => "ITEM DROPPED",
        PlayerActionType.ItemBestowed => "ITEM BESTOWED",
        _ => ActionType.ToString()
    };

    public string FormattedEntry =>
        $"{ActionTypeDisplay} - {PlayerName} | {PlayerId} : {ItemName} {ItemId}";
}