using CharManJur.Models;

namespace CharManJur.Services;

public sealed class FeatureEntry
{
    public ClassFeature Feature { get; init; } = null!;
    public CharacterClass Class { get; init; } = null!;
    public FeatureBehavior Behavior { get; init; } = new();
    public bool IsShapeshift { get; init; }   // the Shifter's transformation, which isn't a feature
    public string StatusText { get; set; } = string.Empty;

    public int Id => Feature.Id;
    public string Name => Feature.Name;
    public string Description => Feature.Description;
    public string ClassName => Class.ClassName;
    public bool IsPassive => Behavior.IsPassive;
    public bool CanUse => !Behavior.IsPassive;

    // Needs the use page: a roll, or a cost the player chooses.
    public bool NeedsInput => IsShapeshift || Behavior.Roll != null || Behavior.Costs.Any(c => c.Variable);
}

public sealed class FeatureWarning { public string Text { get; init; } = string.Empty; }

public sealed class FeatureUseInput
{
    public IReadOnlyList<int> Dice { get; init; } = Array.Empty<int>();
    public int Favour { get; init; }   // the amount for the cost the player chooses (Favour)
}

public sealed class FeatureUseResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

// Live-game handling of class features: what the character has, whether a use is within its limits,
// what it costs, and the log. Limits and requirements are guidelines: they warn, never block.
public class LiveFeatures
{
    private readonly ICharAttribDataService _svc;
    private readonly IClassDataService _classes;
    private readonly IPlayerActionLogService _log;
    private readonly FavourTracker _favour;

    public LiveFeatures(ICharAttribDataService svc, IClassDataService classes, IPlayerActionLogService log, FavourTracker favour)
    {
        _svc = svc;
        _classes = classes;
        _log = log;
        _favour = favour;
    }

    public int DaysPassed => _svc.DaysPassed;
    public int FavourAvailable => _svc.Favour;
    public bool UsesFavour => _svc.UsesFavour;
    public int PoolSize(DicePool pool) => _svc.GetDicePoolSize(pool);
    public bool IsTransformed => _svc.Transform is { IsTransformed: true };

    // ===== LISTING =====
    // Features come from every class the character has picked from, not just the original one.
    // Sub-feature features (Spell Inscription, Technique, Blueprint) live in their own panels.
    public async Task<List<FeatureEntry>> OwnedAsync()
    {
        var all = await _classes.GetClassesAsync();
        var result = new List<FeatureEntry>();

        foreach (var cls in all)
            foreach (var feature in cls.Features)
            {
                if (!_svc.AcquiredFeatureIds.Contains(feature.Id)) continue;
                if (feature.UnlockableTypes.Count > 0) continue;

                var entry = new FeatureEntry { Feature = feature, Class = cls, Behavior = FoundationFeatureBehaviors.For(feature) };
                entry.StatusText = StatusFor(entry);
                result.Add(entry);
            }

        result = result.OrderBy(e => e.ClassName).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();

        // A Shifter's transformation isn't a feature, so it gets its own entry at the top.
        var shifter = all.FirstOrDefault(c => c.Traits.Count > 0 && _svc.ClassSelections.ContainsKey(c.Id));
        if (shifter != null)
        {
            var shape = new FeatureEntry
            {
                Feature = new ClassFeature { Id = -1, Name = "Shapeshift", Description = shifter.Description ?? string.Empty },
                Class = shifter,
                Behavior = new FeatureBehavior(),
                IsShapeshift = true
            };
            shape.StatusText = StatusFor(shape);
            result.Insert(0, shape);
        }

        return result;
    }

    private int UsedThisPeriod(FeatureEntry e)
    {
        var cd = e.Behavior.Cooldown;
        if (cd == null) return 0;
        return _svc.FeatureUses.Count(u => u.FeatureId == e.Id &&
            (cd.Period == CooldownPeriod.ShortRest ? u.ShortRestMark == _svc.ShortRestsTaken : u.DayMark == _svc.DaysPassed));
    }

    private string StatusFor(FeatureEntry e)
    {
        if (e.IsShapeshift)
            return _svc.Transform is { IsTransformed: true } t
                ? $"Transformed{(string.IsNullOrWhiteSpace(t.Name) ? "" : $" as {t.Name}")} · Stability {t.StabilityCurrent}/{t.StabilityMax}"
                : $"Polymorph Dice: {PoolSize(DicePool.Polymorph)}";

        if (e.Behavior.IsPassive) return "Passive · always active";
        if (e.Behavior.Cooldown is { } c) return $"{CooldownText(c)} · used {UsedThisPeriod(e)}/{c.Uses}";
        return "Use any time";
    }

    // ===== TEXT =====
    public static string CooldownText(FeatureCooldown c)
    {
        string count = c.Uses == 1 ? "Once" : c.Uses == 2 ? "Twice" : $"{c.Uses} times";
        return $"{count} per {(c.Period == CooldownPeriod.ShortRest ? "short rest" : "day")}";
    }

    private static List<string> CostLines(FeatureBehavior b)
        => b.Costs.Select(c => c.Type switch
        {
            FeatureCostType.Fatigue => $"{c.Amount} fatigue",
            FeatureCostType.Favour => c.Variable ? "Favour of your choice" : $"{c.Amount} Favour",
            _ => $"{c.Amount}x {c.ItemName ?? "an item"}"
        }).ToList();

    public static string RollRuleText(FeatureRoll r)
    {
        var parts = new List<string>();
        if (r.SpiritLossPerLowDie > 0) parts.Add($"each die showing 1-3 costs {r.SpiritLossPerLowDie} SPI");
        if (r.SpiritLossPerHighDie > 0) parts.Add($"each die showing 4-6 costs {r.SpiritLossPerHighDie} SPI");
        if (r.FatiguePerHighDie > 0) parts.Add($"each die showing 4-6 adds {r.FatiguePerHighDie} fatigue");
        return parts.Count == 0 ? string.Empty : "Every die: " + string.Join("; ", parts) + ".";
    }

    private List<string> ShapeshiftRules()
    {
        var lines = new List<string>
        {
            $"Polymorph Dice: {PoolSize(DicePool.Polymorph)} (re-usable; you can invest up to this many).",
            "Roll the dice you invest: the sum is your Stability, and every die showing 4-6 adds 1 fatigue. There is no series damage.",
            "Traits to pick: your dice + 1, from the lists up to your dice (1 die: PD 1 · 2: PD 1-2 · 3: PD 1-3 · 4: all).",
            "Hit Protection damage hits your Stability instead (armor applies). Then make a Stability save: a failed save, or Stability 0, reverts you.",
            "Your gear transforms with you, but nothing in your pack is accessible. Using it anyway is flagged as rule skating."
        };
        if (IsTransformed) lines.Insert(0, "You are transformed: use Manage form to take damage, roll a save, end combat or revert.");
        return lines;
    }

    public List<string> RulesLines(FeatureEntry e)
    {
        if (e.IsShapeshift) return ShapeshiftRules();

        var b = e.Behavior;
        var lines = new List<string>();
        if (b.IsPassive) { lines.Add("Always active. There's nothing to use."); return lines; }

        if (b.Cooldown is { } c) lines.Add($"Limit: {CooldownText(c)} (a guideline: you can still use it).");

        var costs = CostLines(b);
        if (costs.Count > 0) lines.Add("Cost: " + string.Join(", ", costs));

        if (b.Roll is { } r)
        {
            string pool = DicePools.Name(r.Pool);
            int size = PoolSize(r.Pool);
            lines.Add(r.Variable
                ? $"Roll: choose how many {pool} ({(r.MinDice > 0 ? $"at least {r.MinDice}, " : "")}up to your pool of {size})."
                : $"Roll: {r.MinDice} {pool} (your pool: {size}).");
            string rule = RollRuleText(r);
            if (rule.Length > 0) lines.Add(rule);
        }

        foreach (var cond in b.Conditions)
            lines.Add((cond.Check == ConditionCheck.None ? "Reminder: " : "Requires: ") + cond.Text);

        if (b.IsSustained) lines.Add("Adds an effect chip. Tap it to clear it when it ends.");
        if (b.NotifyParty) lines.Add("Let your party know (it's flagged in your Event Tray).");
        return lines;
    }

    

    // Fills the message tokens from a roll and the Favour invested.
    public static string Fill(string? message, IReadOnlyList<int> dice, int favour)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        int sum = dice.Sum();
        int high = dice.Count(d => d >= 4);
        int low = dice.Count - high;
        return message
            .Replace("[dice]", dice.Count.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[sum]", sum.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[low]", low.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[high]", high.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[favour]", favour.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("[total]", (sum + favour).ToString(), StringComparison.OrdinalIgnoreCase);
    }

    // ===== WARNINGS (guidelines, never blocks) =====
    private bool ConditionMet(ConditionCheck check) => check switch
    {
        ConditionCheck.NoArmorWorn => _svc.GetEquippedArmor() == null,
        ConditionCheck.HoldingShield => _svc.EquippedItems.Any(i =>
            i.EquipmentSlot == EquipmentSlotType.Hand && i.Template?.IsShield == true),
        _ => true
    };

    private int ItemCount(int? templateId)
        => templateId == null ? 0
         : _svc.Inventory.Where(i => !i.IsDropped && !i.TemplateGuid.HasValue && i.TemplateId == templateId.Value).Sum(i => i.Quantity);

    private static string Times(int n) => n == 1 ? "once" : $"{n} times";

    public List<FeatureWarning> Check(FeatureEntry e, int favourInvested)
    {
        var b = e.Behavior;
        var warnings = new List<FeatureWarning>();

        if (b.Cooldown is { } cd)
        {
            int used = UsedThisPeriod(e);
            if (used >= cd.Uses)
                warnings.Add(new FeatureWarning
                {
                    Text = $"{e.Name} is limited to {CooldownText(cd).ToLowerInvariant()}, and you've already used it {Times(used)} " +
                           (cd.Period == CooldownPeriod.ShortRest ? "since your last rest." : "since the last day passed.")
                });
        }

        foreach (var c in b.Conditions.Where(c => c.Check != ConditionCheck.None && !ConditionMet(c.Check)))
            warnings.Add(new FeatureWarning { Text = $"Requirement: {c.Text}. That doesn't look true right now." });

        foreach (var cost in b.Costs)
        {
            int amount = cost.Variable ? favourInvested : cost.Amount;
            if (amount <= 0) continue;

            if (cost.Type == FeatureCostType.Favour && _svc.Favour < amount)
                warnings.Add(new FeatureWarning { Text = $"This uses {amount} Favour and you have {_svc.Favour}." });
            else if (cost.Type == FeatureCostType.Item && ItemCount(cost.ItemId) < amount)
                warnings.Add(new FeatureWarning { Text = $"This needs {amount}x {cost.ItemName ?? "an item"} and you don't have enough." });
        }

        return warnings;
    }

    // The confirm dialog for a feature without choices.
    public string BuildConfirmText(FeatureEntry e, List<FeatureWarning> warnings)
    {
        var blocks = new List<string> { $"Use {e.Name}?" };

        foreach (var w in warnings) blocks.Add("⚠ " + w.Text);
        if (warnings.Count > 0)
            blocks.Add("You can still use it. Your GM may rule otherwise, and it will be flagged in your Event Tray.");

        var costs = CostLines(e.Behavior);
        if (costs.Count > 0) blocks.Add("Cost: " + string.Join(", ", costs));

        foreach (var c in e.Behavior.Conditions.Where(c => c.Check == ConditionCheck.None))
            blocks.Add("Reminder: " + c.Text);

        return string.Join("\n\n", blocks);
    }

    // ===== USE =====
    public async Task<FeatureUseResult> UseAsync(FeatureEntry e, FeatureUseInput input)
    {
        var b = e.Behavior;
        if (b.IsPassive) return Fail($"{e.Name} is always active; there's nothing to use.");

        var dice = input.Dice;
        int chosen = input.Favour;

        // The dice pool is a hard cap.
        if (b.Roll is { } roll)
        {
            int pool = PoolSize(roll.Pool);
            string poolName = DicePools.Name(roll.Pool);
            if (pool < roll.MinDice) return Fail($"You have {pool} {poolName}, but {e.Name} needs at least {roll.MinDice}.");
            if (dice.Count > pool) return Fail($"You only have {pool} {poolName}.");
            if (dice.Count < roll.MinDice || (!roll.Variable && dice.Count != roll.MinDice)) return Fail("Roll the dice first.");
            if (dice.Any(d => d < 1 || d > 6)) return Fail("Every die must be a number from 1 to 6.");
        }
        else if (dice.Count > 0)
        {
            return Fail($"{e.Name} doesn't roll dice.");
        }

        if (b.RequiresInvestment && dice.Count == 0 && chosen <= 0)
            return Fail("Invest at least one die or one Favour.");

        // Anything that doesn't check out is allowed, and logged as rule skating below.
        var warnings = Check(e, chosen);

        // ----- costs -----
        int totalFatigue = 0, favourSpent = 0;
        var itemsUsed = new List<string>();
        foreach (var c in b.Costs)
        {
            int amount = c.Variable ? chosen : c.Amount;
            if (amount <= 0) continue;
            switch (c.Type)
            {
                case FeatureCostType.Fatigue: totalFatigue += amount; break;
                case FeatureCostType.Favour: favourSpent += amount; break;
                case FeatureCostType.Item:
                    if (ConsumeItem(c.ItemId, amount)) itemsUsed.Add($"{amount}x {c.ItemName ?? "item"}");
                    break;
            }
        }
        if (favourSpent > 0) _favour.Adjust(-favourSpent, $"Spent on {e.Name}");

        // ----- the roll's consequences -----
        int spiritLoss = 0;
        if (b.Roll is { } r)
            foreach (int d in dice)
            {
                if (d >= 4) { totalFatigue += r.FatiguePerHighDie; spiritLoss += r.SpiritLossPerHighDie; }
                else spiritLoss += r.SpiritLossPerLowDie;
            }
        if (totalFatigue > 0) _svc.AddFatigue(totalFatigue);
        if (spiritLoss > 0) _svc.AdjustCurrentStat("spirit", -spiritLoss);

        // ----- sustained features leave a chip the player clears when it ends -----
        if (b.IsSustained && !_svc.ActiveEffects.Any(x => x.Name == e.Name))
        {
            _svc.AddEffect(new CharacterEffect
            {
                Name = e.Name,
                Note = "Active. Tap to clear it when it ends.",
                Duration = EffectDurationType.Persistent
            });
        }

        // ----- remember the use (for limits) and tidy old records -----
        _svc.FeatureUses.Add(new FeatureUseRecord
        {
            FeatureId = e.Id,
            ShortRestMark = _svc.ShortRestsTaken,
            DayMark = _svc.DaysPassed
        });
        _svc.FeatureUses.RemoveAll(u => u.ShortRestMark < _svc.ShortRestsTaken && u.DayMark < _svc.DaysPassed);

        // ----- the log -----
        foreach (var w in warnings)
            await LogAsync(PlayerActionType.RuleBypassed, $"Used {e.Name} outside its limits: {w.Text}");

        var detail = new List<string>();
        if (totalFatigue - (b.Roll != null ? dice.Count(d => d >= 4) * b.Roll.FatiguePerHighDie : 0) > 0)
            detail.Add($"fatigue +{totalFatigue - (b.Roll != null ? dice.Count(d => d >= 4) * b.Roll.FatiguePerHighDie : 0)}");
        if (favourSpent > 0) detail.Add($"Favour -{favourSpent}");
        if (itemsUsed.Count > 0) detail.Add("used " + string.Join(", ", itemsUsed));
        if (b.Roll is { } rl && dice.Count > 0) detail.Add($"rolled {dice.Count} {DicePools.Name(rl.Pool)} ({string.Join(", ", dice)})");
        string filled = Fill(b.UseMessage, dice, chosen);

        string summary = $"Used {e.Name} ({e.ClassName})" + (detail.Count > 0 ? " — " + string.Join("; ", detail) : "");
        await LogAsync(PlayerActionType.FeatureUsed, summary, b.NotifyParty);

        await _svc.SaveNowAsync();

        // ----- what the player sees -----
        var parts = new List<string> { filled.Length > 0 ? $"Used {e.Name}. {filled}" : $"Used {e.Name}." };
        if (totalFatigue > 0) parts.Add($"Fatigue +{totalFatigue}.");
        if (spiritLoss > 0) parts.Add($"Spirit -{spiritLoss}.");
        if (b.NotifyParty) parts.Add("Relay this to your party.");
        if (warnings.Count > 0) parts.Add("Noted as rule skating.");
        return new FeatureUseResult { Success = true, Message = string.Join(" ", parts) };
    }

    // ===== DAYS =====
    // Single-player: the player passes days by hand. In multiplayer the GM will trigger this.
    // Ending the day also settles deprivation: a missing Needs Met or Rested chip makes the character
    // Deprived (+1 Fatigue per further day), then both chips clear.
    public async Task<FeatureUseResult> PassDayAsync()
    {
        // Day-limited features only matter if the character has any.
        var all = await _classes.GetClassesAsync();
        bool hasDayFeatures = all
            .SelectMany(c => c.Features)
            .Where(f => _svc.AcquiredFeatureIds.Contains(f.Id))
            .Any(f => FoundationFeatureBehaviors.For(f).Cooldown is { Period: CooldownPeriod.Day });

        var end = _svc.EndDay();
        _svc.DaysPassed++;
        await LogAsync(PlayerActionType.DayPassed, $"A day passes (day {_svc.DaysPassed})");
        await _svc.SaveNowAsync();

        string message = hasDayFeatures
            ? "Needs Met and Rested reset. Daily features are ready again."
            : "Needs Met and Rested reset.";

        if (end.BecameDeprived)
            message += " You are now Deprived.";
        else if (end.StillDeprived && end.FatigueGained > 0)
            message += $" Still Deprived: +{end.FatigueGained} Fatigue.";
        else if (end.StillDeprived)
            message += " Still Deprived.";

        return new FeatureUseResult { Success = true, Message = message };
    }

    // ===== PLUMBING =====
    private bool ConsumeItem(int? templateId, int amount)
    {
        if (templateId == null) return false;

        int remaining = amount;
        var records = _svc.Inventory
            .Where(i => !i.IsDropped && !i.TemplateGuid.HasValue && i.TemplateId == templateId.Value)
            .OrderBy(i => i.Quantity)
            .ToList();

        foreach (var rec in records)
        {
            if (remaining <= 0) break;
            int take = Math.Min(rec.Quantity, remaining);
            if (take >= rec.Quantity)
            {
                _svc.RemoveItemFromInventory(rec.Id);
            }
            else
            {
                rec.Quantity -= take;
                rec.LastModified = DateTime.Now;
            }
            remaining -= take;
        }

        _svc.UpdateEncumbranceState();
        return remaining < amount;
    }

    private Task LogAsync(PlayerActionType type, string summary, bool notifyParty = false)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            NotifyParty = notifyParty,
            Summary = summary
        });

    private static FeatureUseResult Fail(string message) => new() { Success = false, Message = message };
}