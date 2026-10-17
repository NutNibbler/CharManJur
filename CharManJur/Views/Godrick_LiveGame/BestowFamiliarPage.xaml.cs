using System.Text.RegularExpressions;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class BestowFamiliarPage : ContentPage
{
    private readonly IFamiliarDataService _familiarData;
    private readonly ICharAttribDataService _charData;
    private readonly Action _onChanged;

    public BestowFamiliarPage(IFamiliarDataService familiarData, ICharAttribDataService charData, Action onChanged)
    {
        InitializeComponent();
        _familiarData = familiarData;
        _charData = charData;
        _onChanged = onChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var all = await _familiarData.GetAllFamiliarsAsync();
        FamiliarList.ItemsSource = all
            .OrderBy(f => f.FmlrName)
            .Select(f => new BestowFamiliarRow(f))
            .ToList();
    }

    private async void OnBestowClicked(object sender, EventArgs e)
    {
        if (FamiliarList.SelectedItem is not BestowFamiliarRow choice)
        {
            await DisplayAlertAsync("Bestow Familiar", "Select a familiar first.", "OK");
            return;
        }

        _charData.BestowFamiliar(choice.Template);
        _onChanged();
        await Navigation.PopModalAsync();
    }

    private async void OnCreateCustomClicked(object sender, EventArgs e)
    {
        bool bestowed = false;

        var creator = new CustomFamiliarCreatorPage(
            _familiarData,
            created =>
            {
                _charData.BestowFamiliar(created);
                _onChanged();
                bestowed = true;
            },
            showConfirmation: false);

        // The creator announces when it has fully closed; this page is then on top again.
        creator.Closed += async () =>
        {
            if (bestowed) await Navigation.PopModalAsync();
        };

        await Navigation.PushModalAsync(creator);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}

public class BestowFamiliarRow
{
    public Familiar Template { get; }
    public string Name { get; }
    public string Subtitle { get; }
    public string Stats { get; }

    public BestowFamiliarRow(Familiar f)
    {
        Template = f;
        Name = f.FmlrName ?? "(unnamed)";
        Subtitle = string.Join(" · ", new[]
        {
            f.FmlrSize.ToString(),
            f.FmlrClass?.ToString(),
            f.FmlrIntelligence.HasValue ? Regex.Replace(f.FmlrIntelligence.Value.ToString(), "(?<=[a-z])(?=[A-Z])", " ") : null
        }.Where(s => !string.IsNullOrEmpty(s)));
        Stats = $"HP {f.HP}   VIG {f.StatVigor}   AGI {f.StatAgility}   MND {f.StatMind}   SPI {f.StatSpirit}   ·   {f.WeaponSummary}";
    }
}