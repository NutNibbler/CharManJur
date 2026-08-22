using CharManJur.Models;

namespace CharManJur.Views;

public partial class PackEditPopup : ContentPage
{
    private readonly Action<string, string?, string?, PackAssetSyncMode> _onSave;

    public PackEditPopup(string name, string? description, string? details, Action<string, string?, string?, PackAssetSyncMode> onSave)
    {
        InitializeComponent();
        _onSave = onSave;

        entryName.Text = name;
        editorDescription.Text = description;
        editorDetails.Text = details;
        radioNone.IsChecked = true;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Name Required", "Enter a name for the pack.", "OK");
            return;
        }

        PackAssetSyncMode mode = PackAssetSyncMode.None;
        if (radioOverwriteOnly.IsChecked) mode = PackAssetSyncMode.OverwriteOnly;
        else if (radioAddOverwrite.IsChecked) mode = PackAssetSyncMode.AddOverwrite;

        if (mode == PackAssetSyncMode.AddOverwrite)
        {
            bool confirm = await DisplayAlertAsync(
                "Confirm Add/Overwrite",
                "Every currently loaded item will end up in this pack. Any loaded item that currently belongs to a DIFFERENT pack will be REMOVED from that pack and moved into this one.",
                "Continue", "Cancel");

            if (!confirm) return;
        }

        _onSave(entryName.Text.Trim(), editorDescription.Text?.Trim(), editorDetails.Text?.Trim(), mode);
        await Navigation.PopModalAsync();
    }
}