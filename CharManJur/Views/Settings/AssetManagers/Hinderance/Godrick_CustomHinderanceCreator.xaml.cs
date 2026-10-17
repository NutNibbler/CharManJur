using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

[QueryProperty(nameof(HinderanceToEdit), "HinderanceToEdit")]
[QueryProperty(nameof(ReturnRoute), "ReturnRoute")]
public partial class Godrick_CustomHinderanceCreator : ContentPage
{
    private readonly CustomHinderanceBuilderViewModel _viewModel;
    private Hinderance? _hinderanceToEdit;

    public Hinderance? HinderanceToEdit
    {
        get => _hinderanceToEdit;
        set
        {
            _hinderanceToEdit = value;
            if (value != null)
            {
                _viewModel?.LoadHinderanceForEdit(value);
            }
        }
    }

    // Optional — only overrides the ViewModel's default ("//CharBuilder_Godrick_HinderanceSelection")
    // when a caller explicitly supplies it, e.g. a future HinderanceAssetManagerPage passing
    // "//HinderanceAssetManagerPage" so Save/Cancel returns there instead.
    public string? ReturnRoute
    {
        get => _viewModel?.ReturnRoute;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && _viewModel != null)
            {
                _viewModel.ReturnRoute = value;
            }
        }
    }

    public Godrick_CustomHinderanceCreator(IHinderanceDataService hinderanceDataService)
    {
        InitializeComponent();
        _viewModel = new CustomHinderanceBuilderViewModel(hinderanceDataService);
        BindingContext = _viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
    }
}