using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using CharManJur.Views;

namespace CharManJur.Helpers;

// Edit a custom item's definition during play.
//  - Nobody owns it yet: applied now, logged as an ordinary edit, autosaved, toast.
//  - Someone owns it, GM hosting: sent to the GM's approvals tray, nothing changes until approved.
//  - Someone owns it, no GM (single player): applied, and logged as rule skating.
public static class InGameItemEditor
{
    public static async Task EditAsync(ICharAttribDataService svc, IItemDataService items, Item template, Action? onDone = null)
    {
        if (!template.IsPlayerCreated)
        {
            GameNotices.Post(new GameNotice("Can't Edit", "Only custom items can be edited here.", NoticeSource.Player, 4));
            return;
        }

        bool owned = await svc.IsTemplateOwnedAsync(template.Id);
        bool needsApproval = owned && SessionMode.GmHosted;
        bool ruleSkated = owned && !SessionMode.GmHosted;
        IReadOnlyList<string> applied = Array.Empty<string>();

        var viewModel = new CustomItemCreatorViewModel(
            items,
            onItemCreated: saved =>
            {
                svc.RecordItemDefinitionEdit(saved, applied, ruleSkated);
                GameNotices.Post(new GameNotice("Item Updated", $"{saved.Name} was updated.", NoticeSource.Player, 4));
                onDone?.Invoke();
            },
            isLiveGame: true,
            itemToEdit: template,
            editGate: (original, proposed, changes) =>
            {
                if (needsApproval)
                {
                    ItemEditApprovals.Submit(new ItemEditRequest(
                        Guid.NewGuid(), original.Guid, original.Id, original.Name, proposed, changes,
                        svc.CharacterName, DateTime.UtcNow));
                    svc.RecordItemEditRequested(original, changes);
                    GameNotices.Post(new GameNotice("Sent to GM",
                        $"{original.Name} is in use, so your changes need GM approval. Nothing changes until then.",
                        NoticeSource.Player, 6));
                    return Task.FromResult(false);
                }

                applied = changes;
                return Task.FromResult(true);
            });

        await Application.Current!.MainPage!.Navigation.PushModalAsync(new CustomItemCreatorPage(viewModel));
    }
}