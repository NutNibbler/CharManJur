using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class BestowSubFeaturePage : ContentPage
{
    private readonly LiveSubFeatures _live;
    private readonly SubFeatureType _type;
    private readonly Action _onChanged;

    private List<BestowSubFeatureRow> _all = new();

    // Set when a custom entry is created: the bestow runs while the creator closes.
    private Task<SubFeatureResult>? _pendingBestow;

    public BestowSubFeaturePage(LiveSubFeatures live, SubFeatureType type, Action onChanged)
    {
        InitializeComponent();
        _live = live;
        _type = type;
        _onChanged = onChanged;

        string name = LiveSubFeatures.TypeName(type);
        TitleLabel.Text = $"{LiveSubFeatures.Icon(type)} Bestow {name}";
        SearchEntry.Placeholder = $"Search {LiveSubFeatures.PluralName(type).ToLowerInvariant()}...";
        CreateButton.Text = $"✨ Create Custom {name}";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Back from the creator: finish the bestow, then close this page too.
        if (_pendingBestow != null)
        {
            var result = await _pendingBestow;
            _pendingBestow = null;
            if (result.Success)
            {
                _onChanged();
                await Navigation.PopModalAsync();
                return;
            }
            await DisplayAlertAsync("Couldn't Bestow", result.Message, "OK");
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var catalog = await _live.CatalogAsync(_type);
        var owned = _live.Owned(_type);

        _all = catalog
            .Select(e => new BestowSubFeatureRow(e, owned.Any(o => o.IsSameAs(e))))
            .OrderBy(r => r.IsOwned)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string term = SearchEntry.Text?.Trim() ?? string.Empty;
        EntryList.ItemsSource = term.Length == 0
            ? _all
            : _all.Where(r => r.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private async void OnBestowClicked(object sender, EventArgs e)
    {
        if (EntryList.SelectedItem is not BestowSubFeatureRow row)
        {
            await DisplayAlertAsync("Bestow", $"Select a {LiveSubFeatures.TypeName(_type).ToLowerInvariant()} first.", "OK");
            return;
        }

        if (row.IsOwned)
        {
            await DisplayAlertAsync("Already Known", $"You already know {row.Name}.", "OK");
            return;
        }

        var result = await _live.BestowAsync(row.Entry);
        if (!result.Success)
        {
            await DisplayAlertAsync("Couldn't Bestow", result.Message, "OK");
            return;
        }

        _onChanged();
        await Navigation.PopModalAsync();
    }

    private async void OnCreateCustomClicked(object sender, EventArgs e)
    {
        var services = IPlatformApplication.Current!.Services;

        // No class or feature context: a live-game creation isn't part of character creation.
        Page creator = _type switch
        {
            SubFeatureType.Spell => new CustomSpellCreatorPage(
                services.GetRequiredService<ISpellDataService>(), s => OnCustomCreated(s)),
            SubFeatureType.Quip => new CustomQuipCreatorPage(
                services.GetRequiredService<IQuipDataService>(), q => OnCustomCreated(q)),
            SubFeatureType.Blueprint => new CustomBlueprintCreatorPage(
                services.GetRequiredService<IBlueprintDataService>(), b => OnCustomCreated(b)),
            _ => new CustomTechniqueCreatorPage(
                services.GetRequiredService<ITechniqueDataService>(), t => OnCustomCreated(t))
        };

        await Navigation.PushModalAsync(creator);
    }

    // The creators call this before they close themselves.
    private void OnCustomCreated(object created)
        => _pendingBestow = _live.BestowAsync(SubFeatureEntry.From(created), createdJustNow: true);

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}

public class BestowSubFeatureRow
{
    public SubFeatureEntry Entry { get; }
    public bool IsOwned { get; }

    public string Name => Entry.Name;
    public string Description => Entry.Description;
    public string DetailLine => Entry.DetailLine;
    public bool HasDescription => Entry.HasDescription;
    public bool HasDetailLine => Entry.HasDetailLine;

    public BestowSubFeatureRow(SubFeatureEntry entry, bool isOwned)
    {
        Entry = entry;
        IsOwned = isOwned;
    }
}