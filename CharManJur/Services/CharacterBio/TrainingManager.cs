using CharManJur.Models;

namespace CharManJur.Services;

// Live-game training: a pool of unspent points, spent on skills and made permanent by Finish Training.
public sealed class TrainingManager
{
    private readonly ICharAttribDataService _svc;
    private readonly IPlayerActionLogService _log;

    public TrainingManager(ICharAttribDataService svc, IPlayerActionLogService log)
    {
        _svc = svc;
        _log = log;
    }

    public int Unspent => _svc.UnspentTrainingPoints;

    // A player giving themselves points is rule skating (a GM normally awards them).
    public async Task BestowAsync(int points)
    {
        if (points < 1) return;

        _svc.UnspentTrainingPoints += points;
        await LogAsync(PlayerActionType.RuleBypassed,
            $"Bestowed {points} training point(s) on themselves (now {_svc.UnspentTrainingPoints} unspent)");
        await _svc.SaveNowAsync();
        GameNotices.Post(new GameNotice("🎯 Training points awarded",
            $"+{points} training point(s). {_svc.UnspentTrainingPoints} unspent.", NoticeSource.Player, 5));
    }

    public async Task<bool> FinishAsync(IReadOnlyDictionary<string, int> allocation)
    {
        int total = allocation.Values.Where(v => v > 0).Sum();
        if (total < 1 || total > _svc.UnspentTrainingPoints) return false;

        foreach (var (skill, points) in allocation)
        {
            if (points < 1) continue;
            _svc.SkillTrainingLevels.TryGetValue(skill, out int current);
            _svc.SkillTrainingLevels[skill] = current + points;
        }
        _svc.UnspentTrainingPoints -= total;

        var parts = allocation.Where(a => a.Value > 0).OrderBy(a => a.Key).Select(a => $"{a.Key} +{a.Value}");
        await LogAsync(PlayerActionType.TrainingCompleted, $"Finished training ({total} point(s)): {string.Join(", ", parts)}");
        await _svc.SaveNowAsync();
        return true;
    }

    private Task LogAsync(PlayerActionType type, string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            PlayerName = _svc.PlayerName,
            PlayerId = _svc.PlayerId,
            CharacterName = _svc.CharacterName,
            CharacterGuid = _svc.CharacterGuid,
            Summary = summary
        });
}