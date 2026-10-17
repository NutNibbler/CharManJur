using CharManJur.Models;

namespace CharManJur.Views.Godrick_LiveGame;

public record EditItemResult(string? PlayerNote, int? WrittenValueInChips);

// Edits the player-editable parts of one item: its personal note, plus the written value on a Note.
public partial class EditItemPopup : ContentPage
{
    private readonly TaskCompletionSource<EditItemResult?> _completion = new();
    public Task<EditItemResult?> Completion => _completion.Task;

    public EditItemPopup(CharacterItem item, bool canEditValue)
    {
        InitializeComponent();
        TitleLabel.Text = $"Edit {item.DisplayName}";
        NoteEditor.Text = item.PlayerNote;

        ValueSection.IsVisible = canEditValue;
        if (canEditValue)
            ValueEntry.Text = (CurrencyHelper.ValueEach(item) / CurrencyHelper.ChipsPerMark).ToString();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        int? chips = null;
        if (ValueSection.IsVisible)
        {
            if (!CurrencyHelper.TryParseNoteMarks(ValueEntry.Text, out int parsed))
            {
                await DisplayAlertAsync("Invalid Value",
                    $"Enter a whole number of Marks from {CurrencyHelper.MinNoteMarks} to {CurrencyHelper.MaxNoteMarks:N0}.", "OK");
                return;
            }
            chips = parsed;
        }

        _completion.TrySetResult(new EditItemResult(NoteEditor.Text, chips));
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _completion.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(null);   // dismissed any other way (e.g. back button)
    }
}