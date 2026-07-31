using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

namespace CharManJur.Views;

[QueryProperty(nameof(HinderanceToEdit), "HinderanceToEdit")]
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