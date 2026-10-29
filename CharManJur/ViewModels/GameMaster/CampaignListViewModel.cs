using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

public class CampaignRow
{
    public CampaignRow(Campaign campaign)
    {
        Campaign = campaign;
        Detail = $"{campaign.Calendar.DateFor(campaign.Day).Header} · created {campaign.CreatedUtc.ToLocalTime():MMM dd, yyyy}";
    }

    public Campaign Campaign { get; }
    public string Name => Campaign.Name;
    public string Description => Campaign.Description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Campaign.Description);
    public string Detail { get; }
}

public class CampaignListViewModel : INotifyPropertyChanged
{
    private readonly ICampaignStore _store;
    private CampaignRow? _selectedCampaign;

    public ObservableCollection<CampaignRow> Campaigns { get; } = new();

    public CampaignRow? SelectedCampaign
    {
        get => _selectedCampaign;
        set
        {
            _selectedCampaign = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedCampaign));
        }
    }

    public bool HasSelectedCampaign => SelectedCampaign != null;
    public bool HasNoCampaigns => Campaigns.Count == 0;

    public ICommand OpenSelectedCommand { get; }
    public ICommand EditSelectedCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand NewCampaignCommand { get; }

    public CampaignListViewModel(ICampaignStore store)
    {
        _store = store;

        OpenSelectedCommand = new Command(async () => await OpenSelectedAsync());
        EditSelectedCommand = new Command(async () => await EditSelectedAsync());
        DeleteSelectedCommand = new Command(async () => await DeleteSelectedAsync());
        NewCampaignCommand = new Command(async () => await Shell.Current.GoToAsync("///CampaignCreatorPage?campaignId=new"));
    }

    public async Task RefreshAsync()
    {
        var campaigns = await _store.GetCampaignsAsync();

        Campaigns.Clear();
        foreach (var campaign in campaigns) Campaigns.Add(new CampaignRow(campaign));

        SelectedCampaign = null;
        OnPropertyChanged(nameof(HasNoCampaigns));
    }

    // A campaign is its own session: opening it goes straight to the GM dashboard.
    // The visit token tells a real visit from Shell handing the same values over again when a popup closes.
    private async Task OpenSelectedAsync()
    {
        if (SelectedCampaign == null) return;
        await Shell.Current.GoToAsync($"///GmDashboardPage?visit={Guid.NewGuid():N}&campaignId={SelectedCampaign.Campaign.Id}");
    }

    private async Task EditSelectedAsync()
    {
        if (SelectedCampaign == null) return;
        await Shell.Current.GoToAsync($"///CampaignCreatorPage?campaignId={SelectedCampaign.Campaign.Id}");
    }

    private async Task DeleteSelectedAsync()
    {
        if (SelectedCampaign == null) return;

        bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Campaign",
            $"Delete '{SelectedCampaign.Name}'? Its maps, scratch pad, event log and saved character copies are deleted with it. This can't be undone.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        await _store.DeleteCampaignAsync(SelectedCampaign.Campaign.Id);
        await RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}