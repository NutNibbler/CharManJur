using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class BeltSlotSwapPopup : ContentPage
{
    private readonly ICharAttribDataService _charDataService;
    private readonly CharacterItem _item;
    private readonly TaskCompletionSource<bool?> _completion = new();
    private int? _selectedStartSlot;

    public Task<bool?> Completion => _completion.Task;

    public BeltSlotSwapPopup(ICharAttribDataService charDataService, CharacterItem item)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _item = item;

        InstructionLabel.Text = item.SlotsRequired > 1
            ? $"'{item.DisplayName}' needs {item.SlotsRequired} adjacent belt slots. Choose where it goes. Whatever is there returns to your pack."
            : $"Choose which belt slot '{item.DisplayName}' takes. Whatever is there returns to your pack.";

        BuildOptions();
    }

    // One option per valid position, so a bulky item can only ever land on adjacent slots.
    private void BuildOptions()
    {
        int span = _item.SlotsRequired;

        for (int start = 1; start + span - 1 <= 4; start++)
        {
            int startSlot = start;   // captured by the handler below

            var displaced = new List<CharacterItem>();
            for (int slot = start; slot < start + span; slot++)
            {
                var occupant = _charDataService.GetEquippedBeltSlot(slot);
                if (occupant != null && !displaced.Any(d => d.Id == occupant.Id))
                {
                    displaced.Add(occupant);
                }
            }

            string slotText = span > 1 ? $"Slots {start}-{start + span - 1}" : $"Slot {start}";
            string replaces = displaced.Count == 0
                ? "empty"
                : "replaces " + string.Join(", ", displaced.Select(d =>
                    d.SlotsRequired > 1 ? $"{d.DisplayName} (2 slots)" : d.DisplayName));

            var radio = new RadioButton
            {
                GroupName = "BeltChoice",
                Content = $"{slotText}: {replaces}",
                TextColor = Colors.White
            };
            radio.CheckedChanged += (s, e) =>
            {
                if (e.Value) _selectedStartSlot = startSlot;
            };

            OptionsContainer.Children.Add(radio);
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (_selectedStartSlot == null)
        {
            ErrorLabel.Text = "Choose where the item should go.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!_charDataService.EquipToBeltAtSlot(_item.Id, _selectedStartSlot.Value))
        {
            ErrorLabel.Text = "That item couldn't be placed there.";
            ErrorLabel.IsVisible = true;
            return;
        }

        _completion.TrySetResult(true);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _completion.TrySetResult(false);
        await Navigation.PopModalAsync();
    }

    // If the popup is dismissed any other way, don't leave the caller waiting forever.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(false);
    }
}