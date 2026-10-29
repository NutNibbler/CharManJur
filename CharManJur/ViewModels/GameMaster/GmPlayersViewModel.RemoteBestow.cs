using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.ViewModels;

// Bestowing to a player who joined over the network. The GM's own bestow pages are used unchanged, on a fresh
// copy of the player's character; what they do is recorded and sent to the player's app, which repeats it.
public partial class GmPlayersViewModel
{
    private IRemoteBestowService? _remoteBestow;

    private IRemoteBestowService RemoteBestow
        => _remoteBestow ??= IPlatformApplication.Current!.Services.GetRequiredService<IRemoteBestowService>();

    public Task BestowItemRemoteAsync(RemotePlayerRow row)
        => StartRemoteBestowAsync(row, session =>
            new BestowItemPage(_items, session.Svc, session.Log, () => _ = FinishRemoteBestowAsync(row, session)));

    public Task BestowFamiliarRemoteAsync(RemotePlayerRow row)
        => StartRemoteBestowAsync(row, session =>
            new BestowFamiliarPage(_familiars, session.Svc, () => _ = FinishRemoteBestowAsync(row, session)));

    private async Task StartRemoteBestowAsync(RemotePlayerRow row, Func<RemoteBestowSession, Page> makePage)
    {
        if (_busy) return;
        _busy = true;

        try
        {
            // Their app saves and sends a fresh copy first: the bestow page then shows their real familiars.
            var copy = await _net.RequestCopyAsync(row.PlayerId, "bestow", TimeSpan.FromSeconds(10));
            if (!copy.Received || copy.Snapshot == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Can't Bestow", $"{row.Title}: {copy.Message}", "OK");
                return;
            }

            var save = CharacterSnapshots.Unpack(copy.Snapshot);
            if (save == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Can't Bestow", $"{row.Title}'s character copy couldn't be read.", "OK");
                return;
            }

            var session = await RemoteBestow.OpenAsync(save, _runId?.Invoke());
            await PushAsync(makePage(session));
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: starting a bestow to a network player", ex);
            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Can't Bestow", $"Something went wrong getting {row.Title}'s character: {ex.Message}", "OK");
        }
        finally
        {
            _busy = false;
        }
    }

    // The bestow page reported it is done: send what it did, and write its log lines only if the player confirmed.
    private async Task FinishRemoteBestowAsync(RemotePlayerRow row, RemoteBestowSession session)
    {
        if (session.Ops.Count == 0) return;

        string what = session.Summary;
        string json = session.Json;
        session.Ops.Clear();   // a page that reports twice must not send twice

        bool delivered = await SendRemoteAsync(row, "bestow", what, json, null, "It is already in their pack.");
        if (delivered) await session.Log.FlushAsync();
        else session.Log.Discard();
    }

    // One id for every try, so the player's app can tell a resend from a new action. True once it was confirmed.
    private async Task<bool> SendRemoteAsync(
        RemotePlayerRow row, string kind, string what, string json, PlayerActionType? logType, string? note = null)
    {
        try
        {
            var action = new GmActionMessage { ActionId = Guid.NewGuid(), Kind = kind, Json = json };

            while (true)
            {
                var result = await _net.SendActionAsync(row.PlayerId, action, TimeSpan.FromSeconds(8));
                if (result.Delivered)
                {
                    string extra = result.Message.Contains("couldn't") ? $"\n{result.Message}" : string.Empty;
                    GameNotices.Post(new GameNotice("📨 Sent",
                        $"{what} sent to {row.Title}. {note ?? "Their button flashes until they respond."}{extra}",
                        NoticeSource.Player, 6));
                    if (logType.HasValue && _log != null) await _log(logType.Value, $"Sent {what} to {row.Title}");
                    return true;
                }

                bool again = await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Not Confirmed", $"{what} to {row.Title}: {result.Message}", "Send Again", "Cancel");
                if (!again) return false;
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: sending an action to a network player", ex);
            return false;
        }
    }
}