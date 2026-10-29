using CharManJur.ViewModels;

namespace CharManJur.Views;

// Reached with ?campaignId=<guid> to edit a campaign, or ?campaignId=new to create one.
// Shell keeps this page alive between visits, so the campaign is loaded whenever the id arrives.
[QueryProperty(nameof(CampaignId), "campaignId")]
public partial class CampaignCreatorPage : ContentPage
{
    private readonly CampaignCreatorViewModel _viewModel;
    private string? _campaignId;

    public CampaignCreatorPage(CampaignCreatorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public string? CampaignId
    {
        get => _campaignId;
        set
        {
            _campaignId = value;
            _ = _viewModel.LoadAsync(value);
        }
    }
}