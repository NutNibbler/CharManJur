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
        _viewModel.UpdateLimbSetDisplays();
        GenerateLimbSetDisplay();
    }

    private void GenerateLimbSetDisplay()
    {
        LimbSetsGrid.Children.Clear();
        LimbSetsGrid.RowDefinitions.Clear();

        if (_viewModel.LimbSetDisplays == null || _viewModel.LimbSetDisplays.Count == 0)
        {
            LimbSetsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var emptyLabel = new Label
            {
                Text = "No limb sets available",
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetRow(emptyLabel, 0);
            LimbSetsGrid.Children.Add(emptyLabel);
            return;
        }

        // Add a row for each limb set
        for (int i = 0; i < _viewModel.LimbSetDisplays.Count; i++)
        {
            LimbSetsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        int rowIndex = 0;
        foreach (var limbSet in _viewModel.LimbSetDisplays)
        {
            // Create a border for each limb set
            var setBorder = new Border
            {
                Stroke = limbSet.IsOccupiedByTwoHandedItem ? Colors.Orange : Colors.DeepSkyBlue,
                StrokeThickness = limbSet.IsOccupiedByTwoHandedItem ? 2 : 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(8, 5, 8, 5),
                BackgroundColor = limbSet.IsOccupiedByTwoHandedItem
                    ? Color.FromRgba(255, 165, 0, 0.15)
                    : Color.FromRgba(52, 152, 219, 0.1)
            };

            var setLayout = new VerticalStackLayout { Spacing = 3 };

            // Header with icon and name
            var header = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };

            var iconLabel = new Label { Text = limbSet.Icon, FontSize = 14 };
            Grid.SetColumn(iconLabel, 0);

            var nameLabel = new Label
            {
                Text = limbSet.DisplayName,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(nameLabel, 1);

            header.Children.Add(iconLabel);
            header.Children.Add(nameLabel);
            setLayout.Children.Add(header);

            // Slots display
            var slotsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 5
            };

            // Add each slot in this limb set
            for (int i = 0; i < limbSet.Slots.Count; i++)
            {
                var slot = limbSet.Slots[i];
                var slotBorder = new Border
                {
                    Stroke = slot.IsOccupied ? Colors.LightGreen : Colors.Gray,
                    StrokeThickness = slot.IsOccupied ? 2 : 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 4 },
                    Padding = new Thickness(4, 2, 4, 2),
                    BackgroundColor = slot.IsOccupied
                        ? Color.FromRgba(46, 204, 113, 0.2)
                        : Color.FromRgba(128, 128, 128, 0.1)
                };

                // ===== ADD TAP GESTURE FOR CONTEXT MENU =====
                if (slot.IsOccupied)
                {
                    var tapGesture = new TapGestureRecognizer();
                    var slotRef = slot;
                    tapGesture.Tapped += (s, e) =>
                    {
                        // Get the position from the tapped element
                        var element = s as VisualElement;
                        var position = new Point(
                            element?.X ?? 100,
                            element?.Y ?? 100);
                        OnSlotTapped(slotRef, position);
                    };
                    slotBorder.GestureRecognizers.Add(tapGesture);
                    slotBorder.BackgroundColor = Color.FromRgba(46, 204, 113, 0.3);
                }

                var slotLayout = new VerticalStackLayout
                {
                    Spacing = 1,
                    HorizontalOptions = LayoutOptions.Center
                };

                var slotNameLabel = new Label
                {
                    Text = slot.SlotName,
                    FontSize = 8,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                };

                var slotStatusLabel = new Label
                {
                    Text = slot.IsOccupied ? slot.OccupiedByItemName : "Empty",
                    FontSize = 10,
                    TextColor = slot.IsOccupied ? Colors.LightGreen : Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = slot.IsOccupied ? FontAttributes.Bold : FontAttributes.None
                };

                // Show two-handed indicator
                if (slot.IsPartOfTwoHandedItem)
                {
                    var twoHandLabel = new Label
                    {
                        Text = "⚔️ 2H",
                        FontSize = 8,
                        TextColor = Colors.Orange,
                        HorizontalOptions = LayoutOptions.Center,
                        FontAttributes = FontAttributes.Bold
                    };
                    slotLayout.Children.Add(twoHandLabel);
                }

                slotLayout.Children.Add(slotNameLabel);
                slotLayout.Children.Add(slotStatusLabel);
                slotBorder.Content = slotLayout;

                Grid.SetColumn(slotBorder, i);
                slotsGrid.Children.Add(slotBorder);
            }

            setLayout.Children.Add(slotsGrid);
            setBorder.Content = setLayout;

            Grid.SetRow(setBorder, rowIndex);
            LimbSetsGrid.Children.Add(setBorder);

            rowIndex++;
        }
    }

    // ===== HANDLE SLOT TAP - SHOW CONTEXT MENU =====
    // ===== HANDLE SLOT TAP - SHOW CONTEXT MENU =====
    private async void OnSlotTapped(CharacterHomeViewModel.LimbSlotDisplay slot, Point? position = null)
    {
        if (slot == null || !slot.IsOccupied || slot.OccupiedByItem == null)
        {
            return;
        }

        // Get the position of the tapped element
        if (position == null)
        {
            // If no position provided, use center of the screen as fallback
            position = new Point(
                Application.Current.MainPage.Width / 2 - 100,
                Application.Current.MainPage.Height / 2 - 115);
        }

        // Show context menu near the tap position
        var contextMenu = new EquippedItemContextMenu(
            slot.OccupiedByItem,
            _viewModel.CharDataService,
            slot.SlotNumber,
            $"{slot.SlotName} (Slot {slot.SlotNumber})",
            async () =>
            {
                await _viewModel.LoadCharacterDataAsync();
                _viewModel.UpdateLimbSetDisplays();
                GenerateLimbSetDisplay();
            },
            this,
            position);

        await Navigation.PushModalAsync(contextMenu);
    }

    public void SetReturningFromFlyout(bool value)
    {
        _isReturningFromFlyout = value;
    }

    public async void ShowFlyout(CharacterItem characterItem)
    {
        var flyout = new ItemFlyout(
            characterItem,
            _viewModel.CharDataService,
            async () =>
            {
                await _viewModel.LoadCharacterDataAsync();
                _viewModel.UpdateLimbSetDisplays();
                GenerateLimbSetDisplay();
            },
            this);

        await Navigation.PushModalAsync(flyout);
    }

    // ===== DROP MODE ITEM TAP HANDLER =====
    private async void OnDropItemTapped(object sender, TappedEventArgs e)
    {
        if (!_viewModel.IsDropModeActive)
        {
            return;
        }

        if (sender is Border border && border.BindingContext is InventoryItemDisplay item)
        {
            var characterItem = _viewModel.CharDataService.Inventory.FirstOrDefault(i => i.Id == item.Id);
            if (characterItem == null)
            {
                return;
            }

            bool confirm = await DisplayAlertAsync(
                "🫳 Drop Item",
                $"Are you sure you want to drop: '{characterItem.DisplayName}'?",
                "Yes, Drop",
                "Cancel");

            if (confirm)
            {
                _viewModel.CharDataService.DropItem(characterItem.Id);
                await _viewModel.LoadCharacterDataAsync();
                _viewModel.UpdateLimbSetDisplays();
                GenerateLimbSetDisplay();
            }
        }
    }
}