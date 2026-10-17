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

    public CharacterHomePage(CharacterHomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        InitializeDragAndDrop();
        InitializePanels();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CharacterHomeViewModel.LimbSetDisplays))
        {
            GenerateLimbSetDisplay();
        }
        else if (e.PropertyName == nameof(CharacterHomeViewModel.BeltSlotDisplays))
        {
            GenerateBeltSlotDisplay();
        }
        else if (e.PropertyName == nameof(CharacterHomeViewModel.SelectedInventoryItem)
                 && _viewModel.SelectedInventoryItem != null)
        {
            _selectionChangedAt = Environment.TickCount64;   // used by the tile tap toggle
        }
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
        ResetPanels();
        _viewModel.UpdateLimbSetDisplays();
        GenerateLimbSetDisplay();
        GenerateBeltSlotDisplay();
    }

    private static Color GetThemeColor(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
        {
            return color;
        }

        return fallback;
    }

    // Occupied / "good" slots and warning accents follow the active theme.
    private static Color OccupiedColor => GetThemeColor("ThemeConfirmButtonColor", Colors.LightGreen);
    private static Color WarningColor => GetThemeColor("ThemeWarningButtonColor", Colors.Orange);

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
                Stroke = limbSet.IsOccupiedByTwoHandedItem ? WarningColor : GetThemeColor("ThemeSectionBorderColor", Colors.DeepSkyBlue),
                StrokeThickness = limbSet.IsOccupiedByTwoHandedItem ? 2 : 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(8, 5, 8, 5),
                BackgroundColor = limbSet.IsOccupiedByTwoHandedItem
                    ? WarningColor.WithAlpha(0.15f)
                    : GetThemeColor("ThemeSectionBorderColor", Colors.DeepSkyBlue).WithAlpha(0.1f)
            };

            var setLayout = new VerticalStackLayout { Spacing = 3 };

            // Header with icon, name, and (when applicable) a "2H EQUIPPED" chip
            var header = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var iconLabel = new Label { Text = limbSet.Icon, FontSize = 14 };
            Grid.SetColumn(iconLabel, 0);

            // No explicit TextColor: follows the body-text color
            var nameLabel = new Label
            {
                Text = limbSet.DisplayName,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(nameLabel, 1);

            header.Children.Add(iconLabel);
            header.Children.Add(nameLabel);

            if (limbSet.IsOccupiedByTwoHandedItem)
            {
                var twoHandedChip = new Border
                {
                    Stroke = WarningColor,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(6, 1, 6, 1),
                    BackgroundColor = WarningColor.WithAlpha(0.2f),
                    Content = new Label
                    {
                        Text = "2H EQUIPPED",
                        FontSize = 8,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = WarningColor
                    }
                };
                Grid.SetColumn(twoHandedChip, 2);
                header.Children.Add(twoHandedChip);
            }

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

            // A two-handed item occupying the whole set renders as ONE merged cell
            // spanning every column, instead of two separate cells.
            bool isMerged = limbSet.IsOccupiedByTwoHandedItem
                && limbSet.Slots.Count > 1
                && limbSet.Slots[0].IsOccupied;

            if (isMerged)
            {
                var firstSlot = limbSet.Slots[0];
                var lastSlot = limbSet.Slots[limbSet.Slots.Count - 1];

                var mergedBorder = new Border
                {
                    Stroke = OccupiedColor,
                    StrokeThickness = 2,
                    StrokeShape = new RoundRectangle { CornerRadius = 4 },
                    Padding = new Thickness(4, 2, 4, 2),
                    BackgroundColor = OccupiedColor.WithAlpha(0.3f)
                };

                var tapGesture = new TapGestureRecognizer();
                var slotRef = firstSlot;
                tapGesture.Tapped += (s, e) =>
                {
                    var element = s as VisualElement;
                    var position = new Point(
                        element?.X ?? 100,
                        element?.Y ?? 100);
                    OnSlotTapped(slotRef, position);
                };
                mergedBorder.GestureRecognizers.Add(tapGesture);
                AttachHandSlotDragDrop(mergedBorder, firstSlot);
                var mergedLayout = new VerticalStackLayout
                {
                    Spacing = 1,
                    HorizontalOptions = LayoutOptions.Center
                };

                mergedLayout.Children.Add(new Label
                {
                    Text = $"{firstSlot.SlotName} + {lastSlot.SlotName}",
                    FontSize = 8,
                    TextColor = Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                });

                mergedLayout.Children.Add(new Label
                {
                    Text = firstSlot.OccupiedByItemName,
                    FontSize = 10,
                    TextColor = OccupiedColor,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    LineBreakMode = LineBreakMode.TailTruncation
                });

                mergedBorder.Content = mergedLayout;

                Grid.SetColumn(mergedBorder, 0);
                Grid.SetColumnSpan(mergedBorder, slotsGrid.ColumnDefinitions.Count);
                slotsGrid.Children.Add(mergedBorder);
            }
            else
            {
                // Add each slot in this limb set
                for (int i = 0; i < limbSet.Slots.Count; i++)
                {
                    var slot = limbSet.Slots[i];
                    var slotBorder = new Border
                    {
                        Stroke = slot.IsOccupied ? OccupiedColor : GetThemeColor("ThemeDetailBorderColor2", Colors.Gray),
                        StrokeThickness = slot.IsOccupied ? 2 : 1,
                        StrokeShape = new RoundRectangle { CornerRadius = 4 },
                        Padding = new Thickness(4, 2, 4, 2),
                        BackgroundColor = slot.IsOccupied
                            ? OccupiedColor.WithAlpha(0.2f)
                            : GetThemeColor("ThemeDetailBorderColor2", Colors.Gray).WithAlpha(0.1f)
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
                        slotBorder.BackgroundColor = OccupiedColor.WithAlpha(0.3f);
                    }

                    AttachHandSlotDragDrop(slotBorder, slot);

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
                        TextColor = slot.IsOccupied ? OccupiedColor : Colors.Gray,
                        HorizontalOptions = LayoutOptions.Center,
                        FontAttributes = slot.IsOccupied ? FontAttributes.Bold : FontAttributes.None
                    };

                    slotLayout.Children.Add(slotNameLabel);
                    slotLayout.Children.Add(slotStatusLabel);
                    slotBorder.Content = slotLayout;

                    Grid.SetColumn(slotBorder, i);
                    slotsGrid.Children.Add(slotBorder);
                }
            }

            setLayout.Children.Add(slotsGrid);
            setBorder.Content = setLayout;

            Grid.SetRow(setBorder, rowIndex);
            LimbSetsGrid.Children.Add(setBorder);

            rowIndex++;
        }
    }

    private void GenerateBeltSlotDisplay()
    {
        BeltSlotsGrid.Children.Clear();

        foreach (var slot in _viewModel.BeltSlotDisplays)
        {
            var slotBorder = new Border
            {
                Stroke = slot.IsOccupied ? OccupiedColor : GetThemeColor("ThemeDetailBorderColor2", Colors.Gray),
                StrokeThickness = slot.IsOccupied ? 2 : 1,
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                Padding = new Thickness(4, 2, 4, 2),
                BackgroundColor = slot.IsOccupied
                    ? OccupiedColor.WithAlpha(0.2f)
                    : GetThemeColor("ThemeDetailBorderColor2", Colors.Gray).WithAlpha(0.1f)
            };

            if (slot.IsOccupied)
            {
                var tapGesture = new TapGestureRecognizer();
                int slotNumber = slot.SlotNumber;
                tapGesture.Tapped += (s, e) => OnBeltSlotTapped(slotNumber);
                slotBorder.GestureRecognizers.Add(tapGesture);
                slotBorder.BackgroundColor = OccupiedColor.WithAlpha(0.3f);
            }

            AttachBeltSlotDragDrop(slotBorder, slot);

            var slotLayout = new VerticalStackLayout { Spacing = 1, HorizontalOptions = LayoutOptions.Center };

            if (slot.IsPartOfBulkyItem)
            {
                slotLayout.Children.Add(new Label
                {
                    Text = "📦 Bulky",
                    FontSize = 8,
                    TextColor = WarningColor,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold
                });
            }

            string slotLabel = slot.SpanSlots > 1
                ? $"Slots {slot.SlotNumber}-{slot.SlotNumber + slot.SpanSlots - 1}"
                : $"Slot {slot.SlotNumber}";

            slotLayout.Children.Add(new Label
            {
                Text = slotLabel,
                FontSize = 8,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center
            });

            slotLayout.Children.Add(new Label
            {
                Text = slot.IsOccupied ? slot.OccupiedByItemName : "Empty",
                FontSize = 10,
                TextColor = slot.IsOccupied ? OccupiedColor : Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = slot.IsOccupied ? FontAttributes.Bold : FontAttributes.None,
                LineBreakMode = LineBreakMode.TailTruncation
            });

            slotBorder.Content = slotLayout;

            Grid.SetColumn(slotBorder, slot.SlotNumber - 1);
            if (slot.SpanSlots > 1)
            {
                Grid.SetColumnSpan(slotBorder, slot.SpanSlots);
            }

            BeltSlotsGrid.Children.Add(slotBorder);
        }
    }

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

    private async void OnBeltSlotTapped(int slotNumber)
    {
        var item = _viewModel.CharDataService.GetEquippedBeltSlot(slotNumber);
        if (item == null) return;

        int totalHandSlots = _viewModel.CharDataService.GetTotalLimbSlots();

        var contextMenu = new EquippedItemContextMenu(
            item,
            _viewModel.CharDataService,
            totalHandSlots + slotNumber,
            $"Belt Slot {slotNumber}",
            async () =>
            {
                await _viewModel.LoadCharacterDataAsync();
                _viewModel.UpdateLimbSetDisplays();
                GenerateLimbSetDisplay();
            },
            this);

        await Navigation.PushModalAsync(contextMenu);
    }

    private async void OnArmorTapped(object sender, TappedEventArgs e)
    {
        var item = _viewModel.CharDataService.GetEquippedArmor();
        if (item == null) return;

        var contextMenu = new EquippedItemContextMenu(
            item,
            _viewModel.CharDataService,
            1,
            "Armor",
            async () =>
            {
                await _viewModel.LoadCharacterDataAsync();
                _viewModel.UpdateLimbSetDisplays();
                GenerateLimbSetDisplay();
                GenerateBeltSlotDisplay();
            },
            this);

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
        if (BindingContext is CharacterHomeViewModel selVm
            && !selVm.IsDropModeActive
            && (sender as BindableObject)?.BindingContext is InventoryItemDisplay tappedItem)
        {
            selVm.SelectedInventoryItem = tappedItem;
        }
        if (!_viewModel.IsDropModeActive || _viewModel.IsPackDropped)
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

            if (await ItemDropHelper.DropAsync(characterItem, _viewModel.CharDataService))
            {
                await _viewModel.LoadCharacterDataAsync();
                _viewModel.UpdateLimbSetDisplays();
                GenerateLimbSetDisplay();
            }
        }
    }
}