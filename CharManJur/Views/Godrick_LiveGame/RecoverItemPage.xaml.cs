using CharManJur.Models;
using CharManJur.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class RecoverItemPage : ContentPage, INotifyPropertyChanged
{
    private readonly ICharAttribDataService _charDataService;
    private readonly Action? _onItemRecovered;
    private ObservableCollection<CharacterItem> _recoverableItems = new();
    private CharacterItem? _selectedItem;

    public ObservableCollection<CharacterItem> RecoverableItems
    {
        get => _recoverableItems;
        set { _recoverableItems = value; OnPropertyChanged(); }
    }

    public CharacterItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedItem));
        }
    }

    public bool HasSelectedItem => SelectedItem != null;

    public RecoverItemPage(ICharAttribDataService charDataService, Action? onItemRecovered = null)
    {
        InitializeComponent();
        _charDataService = charDataService;
        _onItemRecovered = onItemRecovered;
        BindingContext = this;

        LoadRecoverables();
    }

    private async void LoadRecoverables()
    {
        try
        {
            var items = await _charDataService.GetRecoverableItemsAsync();
            RecoverableItems = new ObservableCollection<CharacterItem>(items);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading recoverables: {ex.Message}");
            await DisplayAlertAsync("Error", $"Failed to load recoverable items: {ex.Message}", "OK");
        }
    }

    private void OnItemSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItem = e.CurrentSelection.FirstOrDefault() as CharacterItem;
    }

    private async void OnRecoverClicked(object sender, EventArgs e)
    {
        if (SelectedItem == null) return;

        bool confirm = await DisplayAlertAsync(
            "Recover Item",
            $"Recover '{SelectedItem.DisplayName}' back into your inventory?",
            "Yes, Recover",
            "Cancel");

        if (!confirm) return;

        bool success = await _charDataService.RecoverItemAsync(SelectedItem.InstanceId);

        if (success)
        {
            _onItemRecovered?.Invoke();
            await DisplayAlertAsync("Recovered!",
                $"'{SelectedItem.DisplayName}' has been returned to your inventory.", "OK");
            await Navigation.PopModalAsync();
        }
        else
        {
            await DisplayAlertAsync("Error",
                "Could not recover this item. It may have already been recovered.", "OK");
            LoadRecoverables();
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}