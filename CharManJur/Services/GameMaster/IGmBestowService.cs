using CharManJur.Helpers;
using CharManJur.Models;

namespace CharManJur.Services;

public enum GmBestowKind { Item, Familiar, Spell, Quip, Technique, Blueprint, ScratchPage }

// One line of the GM's basket: something to give, with the options chosen for it.
public sealed class GmBestowLine
{
    public GmBestowKind Kind { get; init; }
    public Item? Item { get; init; }
    public Familiar? Familiar { get; init; }
    public SubFeatureEntry? SubFeature { get; init; }
    public ScratchPageInfo? Page { get; init; }
    public Guid CampaignId { get; init; }         // which campaign's pad a page comes from
    public int Quantity { get; init; } = 1;
    public int? WrittenValue { get; init; }       // chips, for a Note
    public int? UsageDieSides { get; init; }

    public string Name => Kind switch
    {
        GmBestowKind.Item => Item?.Name ?? string.Empty,
        GmBestowKind.Familiar => Familiar?.FmlrName ?? "(unnamed)",
        GmBestowKind.ScratchPage => Page?.Name ?? string.Empty,
        _ => SubFeature?.Name ?? string.Empty
    };

    public string Title => Kind == GmBestowKind.Item && Quantity > 1 ? $"{Quantity}× {Name}" : Name;

    public string KindLabel => Kind switch
    {
        GmBestowKind.Item => "Item",
        GmBestowKind.Familiar => "Familiar or Construct",
        GmBestowKind.Spell => "Spell",
        GmBestowKind.Quip => "Quip",
        GmBestowKind.Technique => "Technique",
        GmBestowKind.ScratchPage => "Scratch Pad Page",
        _ => "Blueprint"
    };

    public string Subtitle
    {
        get
        {
            var parts = new List<string> { KindLabel };
            if (Page != null) parts.Add(Page.Type == ScratchPageType.Drawing ? "Drawing" : "Text");
            if (WrittenValue.HasValue) parts.Add($"{WrittenValue.Value / CurrencyHelper.ChipsPerMark} Marks written");
            if (UsageDieSides.HasValue) parts.Add($"{UsageDie.Label(UsageDieSides.Value)} die");
            return string.Join(" · ", parts);
        }
    }
}

// What happened for one player.
public sealed class GmBestowOutcome
{
    public string Recipient { get; init; } = string.Empty;
    public List<string> Given { get; } = new();
    public List<string> Notes { get; } = new();
    public string? Error { get; set; }

    public string Summary
    {
        get
        {
            if (Error != null) return $"{Recipient}: {Error}";

            var parts = new List<string>();
            if (Given.Count > 0) parts.Add("gave " + string.Join(", ", Given));
            parts.AddRange(Notes);
            return $"{Recipient}: " + (parts.Count > 0 ? string.Join("; ", parts) : "nothing changed");
        }
    }
}

public interface IGmBestowService
{
    Task<List<Item>> GetItemsAsync();
    Task<List<Familiar>> GetFamiliarsAsync();
    Task<List<SubFeatureEntry>> GetSubFeaturesAsync(SubFeatureType type);
    Task<List<ScratchPageInfo>> GetPadPagesAsync(Guid campaignId);

    // Which of the recipients would end up encumbered if the item lines were given. Nothing is saved or logged.
    Task<List<string>> PreviewEncumbranceAsync(IReadOnlyList<CharacterSaveData> recipients, IReadOnlyList<GmBestowLine> lines);

    // Gives every line to every recipient, one player at a time. Each player's saved character is changed on disk.
    Task<List<GmBestowOutcome>> BestowAsync(IReadOnlyList<CharacterSaveData> recipients, IReadOnlyList<GmBestowLine> lines, Guid? sessionId);
}