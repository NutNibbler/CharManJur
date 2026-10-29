using System;
using System.Collections.Generic;
using System.Linq;

namespace CharManJur.Models;

// Moves item records between containers (character pack <-> familiar pack).
// Uses, notes and custom names travel with the item; stackables merge into partial stacks.
//
// Uses on a stack: RemainingUses belongs to the stack's one opened item; every other item in
// it is full. Two stacks merge when neither is empty and at most one is opened: full units
// joining an opened stack change nothing, and an opened item joining a full stack gives the
// merged stack its uses. When only PART of a stack moves, the opened item stays with its
// source and only full units travel.
public static class ItemRecordTransfer
{
    public static bool SameItem(CharacterItem a, CharacterItem b) =>
        a.TemplateGuid.HasValue || b.TemplateGuid.HasValue
            ? a.TemplateGuid == b.TemplateGuid
            : a.TemplateId == b.TemplateId;

    public static int FullUses(CharacterItem item) =>
        item.Template != null && item.Template.HasUses ? item.Template.Uses!.Value : 0;

    public static bool IsEmptyItem(CharacterItem item) =>
        FullUses(item) > 0 && item.RemainingUses <= 0;

    public static bool IsOpened(CharacterItem item)
    {
        int full = FullUses(item);
        return full > 0 && item.RemainingUses > 0 && item.RemainingUses < full;
    }

    // Merged stacks keep both notes: existing first, then incoming.
    public static string? CombineNotes(string? existing, string? incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming)) return existing;
        if (string.IsNullOrWhiteSpace(existing)) return incoming;
        if (string.Equals(existing.Trim(), incoming.Trim(), StringComparison.Ordinal)) return existing;
        return existing + Environment.NewLine + incoming;
    }

    // Copies `quantity` of `source` into `destination`. Does not touch the source.
    public static void MoveInto(IList<CharacterItem> destination, CharacterItem source, int quantity, Func<int> nextId)
    {
        if (source.Template == null) return;

        quantity = Math.Clamp(quantity, 1, Math.Max(1, source.Quantity));
        bool stackable = source.Template.IsStackableItem;
        int cap = InventorySlotCalculator.MaxPerRecord(source.Template);
        int fullUses = FullUses(source);

        // The opened item only travels when the whole stack does; otherwise full units move.
        bool wholeStack = quantity >= source.Quantity;
        int incomingUses = !wholeStack && IsOpened(source) ? fullUses : source.RemainingUses;
        bool incomingEmpty = fullUses > 0 && incomingUses <= 0;
        bool incomingOpened = fullUses > 0 && incomingUses > 0 && incomingUses < fullUses;

        int remaining = quantity;
        bool notePlaced = false;   // the incoming note is attached once, not once per record
        bool usesPlaced = false;   // the opened item lands in one record only

        if (stackable && !incomingEmpty)
        {
            foreach (var stack in destination.Where(d =>
                         SameItem(d, source) && !d.IsDropped && !d.IsEquipped &&
                         d.Quantity < cap && !IsEmptyItem(d)))
            {
                if (remaining <= 0) break;

                bool carriesOpened = incomingOpened && !usesPlaced;
                if (carriesOpened && IsOpened(stack)) continue;   // both opened: can't merge

                int add = Math.Min(remaining, cap - stack.Quantity);
                stack.Quantity += add;
                stack.LastModified = DateTime.Now;
                remaining -= add;

                if (carriesOpened)
                {
                    stack.RemainingUses = incomingUses;
                    usesPlaced = true;
                }

                if (!notePlaced)
                {
                    stack.PlayerNote = CombineNotes(stack.PlayerNote, source.PlayerNote);
                    notePlaced = true;
                }
            }
        }

        while (remaining > 0)
        {
            int chunk = stackable ? Math.Min(remaining, cap) : 1;
            int uses = usesPlaced ? fullUses : incomingUses;
            usesPlaced = true;

            destination.Add(new CharacterItem
            {
                Id = nextId(),
                InstanceId = (quantity == source.Quantity && chunk == quantity) ? source.InstanceId : Guid.NewGuid(),
                TemplateId = source.TemplateId,
                TemplateGuid = source.TemplateGuid,
                Template = source.Template,
                Quantity = chunk,
                RemainingUses = uses,
                UsageDieSides = source.UsageDieSides,
                IsEmpty = fullUses > 0 && uses <= 0,
                IsIdentified = source.IsIdentified,
                CustomName = source.CustomName,
                PlayerNote = notePlaced ? null : source.PlayerNote,
                WrittenValueInChips = source.WrittenValueInChips,
                AcquiredAt = source.AcquiredAt
            });
            notePlaced = true;
            remaining -= chunk;
        }
    }

    // Takes `quantity` out of a record, removing the record when it reaches zero.
    public static void RemoveQuantity(IList<CharacterItem> list, CharacterItem record, int quantity)
    {
        if (quantity >= record.Quantity)
        {
            list.Remove(record);
        }
        else
        {
            record.Quantity -= quantity;
            record.LastModified = DateTime.Now;
        }
    }
}