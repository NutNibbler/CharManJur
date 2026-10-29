using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public partial class CharacterHomeViewModel
{
    private WeaponProficiencyTracker? _proficiency;
    public WeaponProficiencyTracker Proficiency
        => _proficiency ??= new WeaponProficiencyTracker(_charDataService, _actionLogService);

    // ===== +KILL =====
    // The +Kill button on a held weapon in the Wielded Weapons list. The parameter is the CharacterItem id.
    private ICommand? _recordKillCommand;
    public ICommand RecordKillCommand => _recordKillCommand ??= new Command<int>(async id => await OnRecordKillAsync(id));

    private async Task OnRecordKillAsync(int itemId)
    {
        var item = _charDataService.Inventory.FirstOrDefault(i =>
            i.Id == itemId && i.IsEquipped && i.EquipmentSlot == EquipmentSlotType.Hand);
        if (item?.Template is not { IsWeapon: true, DamageType: not null }) return;

        var result = await Proficiency.RecordKillAsync(item.Template);
        await LoadCharacterDataAsync();

        if (result.Unlocked) ShowToast("🎓 Proficiency unlocked", result.Message, 8);
        else if (result.Recorded) ShowToast("☠️ Kill recorded", result.Message, 4);
        else ShowToast("☠️ Kill", result.Message, 4);
    }

    // ===== STARTING PROFICIENCY (asked once, on first load) =====
    private bool _startingPromptRunning;
    private Guid _startingPromptDeclinedFor;

    private async Task PromptStartingProficiencyAsync()
    {
        var tracker = Proficiency;
        if (_startingPromptRunning || !tracker.NeedsStartingChoice || _startingPromptDeclinedFor == _charDataService.CharacterGuid) return;

        _startingPromptRunning = true;
        try
        {
            await Task.Delay(600);   // let the page finish appearing

            var page = Application.Current?.MainPage;
            if (page == null || !tracker.NeedsStartingChoice || page.Navigation.ModalStack.Count > 0) return;

            if (await ProficiencyPrompts.AskStartingTypeAsync(tracker))
                await LoadCharacterDataAsync();
            else
                _startingPromptDeclinedFor = _charDataService.CharacterGuid;   // the Bio Sheet has a button for it
        }
        finally { _startingPromptRunning = false; }
    }
}