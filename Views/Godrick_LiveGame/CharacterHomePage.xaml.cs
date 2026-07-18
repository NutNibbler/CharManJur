using CharManJur.Converters;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;
using Microsoft.Maui.Controls.Shapes;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CharacterHomePage : ContentPage
{
    private readonly CharacterHomeViewModel _viewModel;
    private bool _isReturningFromFlyout = false;
    private List<Border> _handSlotBorders = new();

    public CharacterHomePage(CharacterHomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isReturningFromFlyout)
        {
            _isReturningFromFlyout = false;
            return;
        }

        await _viewModel.LoadCharacterDataAsync();
        GenerateHandSlots();
    }

    private void GenerateHandSlots()
    {
        // Clear existing hand slots
        HandSlotsGrid.Children.Clear();
        _handSlotBorders.Clear();

        int totalSlots = _viewModel.TotalHandSlots;

        // Calculate rows needed (2 columns per row)
        int rows = (totalSlots + 1) / 2;
        if (rows == 0) rows = 1;

        // Set row definitions
        HandSlotsGrid.RowDefinitions.Clear();
        for (int i = 0; i < rows; i++)
        {
            HandSlotsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        // Generate each hand slot
        for (int i = 0; i < totalSlots; i++)
        {
            int row = i / 2;
            int col = i % 2;
            int slotNumber = i + 1;

            var border = new Border
            {
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Padding = new Thickness(5),
                BackgroundColor = Colors.Transparent
            };

            // Set binding for the slot display
            var label = new Label
            {
                FontSize = 12,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            };

            // Bind to the appropriate hand slot property
            string bindingPath = i switch
            {
                0 => "HandSlot1",
                1 => "HandSlot2",
                2 => "HandSlot3",
                3 => "HandSlot4",
                _ => $"HandSlot{i + 1}"
            };
            label.SetBinding(Label.TextProperty, new Binding(bindingPath));

            // Set border color based on bulky item
            string bulkyBindingPath = i switch
            {
                0 => "HandSlot1Bulky",
                1 => "HandSlot2Bulky",
                2 => "HandSlot3Bulky",
                3 => "HandSlot4Bulky",
                _ => $"HandSlot{i + 1}Bulky"
            };
            border.SetBinding(Border.StrokeProperty, new Binding(bulkyBindingPath, converter: new BulkyBorderConverter()));
            border.SetBinding(Border.StrokeThicknessProperty, new Binding(bulkyBindingPath, converter: new BulkyBorderThicknessConverter()));
            border.SetBinding(Border.BackgroundColorProperty, new Binding(bulkyBindingPath, converter: new BulkyBackgroundConverter()));

            border.Content = label;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            HandSlotsGrid.Children.Add(border);
            _handSlotBorders.Add(border);
        }
    }

    public void SetReturningFromFlyout(bool value)
    {
        _isReturningFromFlyout = value;
    }

    public async void ShowFlyout(CharacterItem characterItem)
    {
        var flyout = new ItemFlyout(characterItem, _viewModel.CharDataService, async () =>
        {
            await _viewModel.LoadCharacterDataAsync();
            GenerateHandSlots();
        }, this);

        await Navigation.PushModalAsync(flyout);
    }

    // ===== DESTROY MODE ITEM TAP HANDLER =====
    private async void OnDestroyItemTapped(object sender, TappedEventArgs e)
    {
        // Only process if destroy mode is active
        if (!_viewModel.IsDestroyModeActive) return;

        // Get the item from the tapped border
        if (sender is Border border && border.BindingContext is InventoryItemDisplay item)
        {
            // Find the CharacterItem from the service
            var characterItem = _viewModel.CharDataService.Inventory.FirstOrDefault(i => i.Id == item.Id);
            if (characterItem == null) return;

            // Show confirmation dialog
            bool confirm = await DisplayAlertAsync(
                "🗑️ Destroy Item",
                $"Are you sure you want to permanently destroy '{characterItem.DisplayName}'?",
                "Yes, Destroy",
                "Cancel");

            if (confirm)
            {
                _viewModel.CharDataService.RemoveItemFromInventory(characterItem.Id);
                await _viewModel.LoadCharacterDataAsync();
                GenerateHandSlots();
            }
        }
    }
}