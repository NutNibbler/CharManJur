using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views;

// The GM's "Send Damage": how much, what kind and how fast. It hands the damage back to be sent; nothing is applied here.
public partial class GmSendDamagePage : ContentPage
{
    private readonly Action<IncomingDamage> _onComposed;

    public GmSendDamagePage(string recipientName, Action<IncomingDamage> onComposed)
    {
        InitializeComponent();
        _onComposed = onComposed;

        RecipientLabel.Text = $"To {recipientName}";
        TypePicker.SelectedIndex = 0;
        SpeedPicker.SelectedIndex = (int)IncomingAttackSpeed.Balanced;
    }

    // Attack speed only matters for Hitpoints damage (the kind a player can Block or Dodge).
    private void OnTypeChanged(object sender, EventArgs e)
        => SpeedSection.IsVisible = TypePicker.SelectedIndex == (int)DamageTargetType.Hitpoints;

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (TypePicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Missing Type", "Choose a damage type.", "OK");
            return;
        }

        if (!int.TryParse(AmountEntry.Text, out int amount) || amount < 1)
        {
            await DisplayAlertAsync("Invalid Amount", "Enter a whole number of 1 or more.", "OK");
            return;
        }

        string note = NoteEntry.Text?.Trim() ?? string.Empty;

        _onComposed(new IncomingDamage
        {
            Target = (DamageTargetType)TypePicker.SelectedIndex,
            Amount = amount,
            Speed = (IncomingAttackSpeed)Math.Max(0, SpeedPicker.SelectedIndex),
            Note = note.Length == 0 ? null : note
        });

        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}