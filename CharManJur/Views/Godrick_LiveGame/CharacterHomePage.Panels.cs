using CharManJur.ViewModels.Godrick_LiveGame;
using CharManJur.Services;
using CharManJur.Models;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CharacterHomePage
{
    private enum OverlayPanel { None, Familiars, Shop, Spells, Quips, Blueprints, Techniques, Features, Scratch }

    private OverlayPanel _openPanel = OverlayPanel.None;
    private bool _panelBusy;
    private FamiliarPanelViewModel? _familiarPanelVm;
    private ShopPanelViewModel? _shopPanelVm;
    private LiveSubFeatures? _liveSubFeatures;
    private SubFeaturePanelViewModel? _subFeaturePanelVm;
    private MagicCasting? _magicCasting;
    private CraftService? _craftService;
    private LiveFeatures? _liveFeatures;
    private FeaturePanelViewModel? _featurePanelVm;

    private void InitializePanels()
    {
        _familiarPanelVm = new FamiliarPanelViewModel(_viewModel.CharDataService);
        _familiarPanelVm.CharacterInventoryChanged += async () => await _viewModel.LoadCharacterDataAsync();
        _familiarPanelVm.BestowFamiliarRequested += OnBestowFamiliarRequested;
        FamiliarPanel.BindingContext = _familiarPanelVm;

        IServiceProvider services = new PageServices(IPlatformApplication.Current!.Services, _viewModel.ActionLog);

        _shopPanelVm = new ShopPanelViewModel(
            _viewModel.CharDataService,
            services.GetRequiredService<IItemDataService>(),
            services.GetRequiredService<IPlayerActionLogService>());
        _shopPanelVm.CharacterInventoryChanged += async () => await _viewModel.LoadCharacterDataAsync();
        _shopPanelVm.ModeChanged += () =>
        {
            if (_openPanel == OverlayPanel.Shop) PanelTitleLabel.Text = _shopPanelVm.ModeTitle;
        };
        ShopPanel.BindingContext = _shopPanelVm;

        _liveSubFeatures = new LiveSubFeatures(
            _viewModel.CharDataService,
            services.GetRequiredService<IBlueprintDataService>(),
            services.GetRequiredService<IQuipDataService>(),
            services.GetRequiredService<ISpellDataService>(),
            services.GetRequiredService<ITechniqueDataService>(),
            services.GetRequiredService<IPlayerActionLogService>());
        _subFeaturePanelVm = new SubFeaturePanelViewModel(_liveSubFeatures);
        _subFeaturePanelVm.BestowRequested += OnBestowSubFeatureRequested;
        _magicCasting = new MagicCasting(
            _viewModel.CharDataService,
            services.GetRequiredService<IPlayerActionLogService>());
        _subFeaturePanelVm.CastRequested += OnCastRequested;
        _craftService = new CraftService(
        _viewModel.CharDataService,
            services.GetRequiredService<IItemDataService>(),
            services.GetRequiredService<IPlayerActionLogService>());
        _subFeaturePanelVm.CraftRequested += OnCraftRequested;
        _liveFeatures = new LiveFeatures(
            _viewModel.CharDataService,
            services.GetRequiredService<IClassDataService>(),
            services.GetRequiredService<IPlayerActionLogService>(),
            _viewModel.FavourTracker);
        _featurePanelVm = new FeaturePanelViewModel(_liveFeatures, _viewModel.FavourTracker);
        _featurePanelVm.UseRequested += OnFeatureUseRequested;
        _featurePanelVm.CharacterChanged += async () => await _viewModel.LoadCharacterDataAsync();
        _viewModel.PassDayHandler = async () =>
        {
            var result = await _liveFeatures!.PassDayAsync();
            if (_featurePanelVm != null) await _featurePanelVm.RefreshAsync();   // Days passed, once-per-day statuses
            return result;
        };
        FeaturePanel.BindingContext = _featurePanelVm;
        _viewModel.TransformationChanged += async () =>
        {
            if (_featurePanelVm != null) await _featurePanelVm.RefreshAsync();
        };
        _subFeaturePanelVm.TypeChanged += type =>
        {
            // The Spells | Techniques switch changed the type inside the open panel.
            if (SubFeatureTypeFor(_openPanel) == null) return;
            _openPanel = PanelFor(type);
            PanelTitleLabel.Text = _subFeaturePanelVm.Title;
        };
        SubFeaturePanel.BindingContext = _subFeaturePanelVm;

        // Keep the overlay exactly as tall as the panels it covers, so it never resizes the row.
        EquipInvGrid.SizeChanged += (_, _) =>
        {
            if (_openPanel != OverlayPanel.None && !_panelBusy && EquipInvGrid.Height > 0)
                PanelOverlay.HeightRequest = EquipInvGrid.Height;
        };
        _viewModel.ModalOpening += () => SetReturningFromFlyout(true);
        InitializeEventTray();
        InitializeTileToggle();
        InitializeBioFlash();
        InitializeIncomingFlash();
        InitializeScratchPad();
    }

    // Called when a different character is loaded into this page.
    private void ResetPanels()
    {
        _ = SaveScratchPadAsync();
        ResetEventTray();
        PanelOverlay.IsVisible = false;
        PanelOverlay.TranslationY = 0;
        _openPanel = OverlayPanel.None;
        SetBackgroundPanelsCovered(false);
    }

    private static SubFeatureType? SubFeatureTypeFor(OverlayPanel panel) => panel switch
    {
        OverlayPanel.Spells => SubFeatureType.Spell,
        OverlayPanel.Quips => SubFeatureType.Quip,
        OverlayPanel.Blueprints => SubFeatureType.Blueprint,
        OverlayPanel.Techniques => SubFeatureType.Technique,
        _ => null
    };

    private static OverlayPanel PanelFor(SubFeatureType type) => type switch
    {
        SubFeatureType.Spell => OverlayPanel.Spells,
        SubFeatureType.Quip => OverlayPanel.Quips,
        SubFeatureType.Blueprint => OverlayPanel.Blueprints,
        _ => OverlayPanel.Techniques
    };

    private async void OnFamiliarsClicked(object? sender, EventArgs e)
        => await TogglePanelAsync(OverlayPanel.Familiars);

    private async void OnSpellsClicked(object? sender, EventArgs e)
        => await TogglePanelAsync(OverlayPanel.Spells);

    private async void OnQuipsClicked(object? sender, EventArgs e)
        => await TogglePanelAsync(OverlayPanel.Quips);

    private async void OnFeaturesClicked(object? sender, EventArgs e)
    => await TogglePanelAsync(OverlayPanel.Features);

    private async void OnBlueprintsClicked(object? sender, EventArgs e)
        => await TogglePanelAsync(OverlayPanel.Blueprints);

    private async void OnShopClicked(object? sender, EventArgs e)
    {
        // A GM host can close the shop for the session (multiplayer, not built yet).
        if (!ShopSettings.IsOpen && _openPanel != OverlayPanel.Shop)
        {
            await DisplayAlertAsync("Shop Closed", "The shop isn't open right now.", "OK");
            return;
        }
        await TogglePanelAsync(OverlayPanel.Shop);
    }

    private async void OnClosePanelClicked(object? sender, EventArgs e)
    {
        if (_openPanel == OverlayPanel.None) return;
        await TogglePanelAsync(_openPanel);
    }

    private async Task TogglePanelAsync(OverlayPanel panel)
    {
        if (_panelBusy) return;
        _panelBusy = true;
        try
        {
            if (_openPanel == panel)
                await HidePanelAsync();
            else
                await ShowPanelAsync(panel);
        }
        finally
        {
            _panelBusy = false;
        }
    }

    private async Task ShowPanelAsync(OverlayPanel panel)
    {
        if (_openPanel == OverlayPanel.Scratch && panel != OverlayPanel.Scratch) await SaveScratchPadAsync();
        // Already open on another panel: swap the content in place instead of sliding again.
        bool slideIn = _openPanel == OverlayPanel.None;
        var subType = SubFeatureTypeFor(panel);

        FamiliarPanel.IsVisible = panel == OverlayPanel.Familiars;
        ShopPanel.IsVisible = panel == OverlayPanel.Shop;
        SubFeaturePanel.IsVisible = subType.HasValue;
        FeaturePanel.IsVisible = panel == OverlayPanel.Features;
        ScratchPanel.IsVisible = panel == OverlayPanel.Scratch;

        if (panel == OverlayPanel.Familiars)
        {
            _familiarPanelVm?.Refresh();
            PanelTitleLabel.Text = "Familiars & Constructs";
        }
        else if (panel == OverlayPanel.Shop)
        {
            if (_shopPanelVm != null) await _shopPanelVm.RefreshAsync();   // always opens on Buy
            PanelTitleLabel.Text = _shopPanelVm?.ModeTitle ?? "🏪 Shop";
        }
        else if (panel == OverlayPanel.Features)
        {
            if (_featurePanelVm != null) await _featurePanelVm.RefreshAsync();
            PanelTitleLabel.Text = "⚙️ Features";
        }
        else if (panel == OverlayPanel.Scratch)
        {
            await OpenScratchPadAsync();
            PanelTitleLabel.Text = "📝 Scratch Pad";
        }
        else if (subType.HasValue && _subFeaturePanelVm != null)
        {
            _subFeaturePanelVm.SetType(subType.Value);
            PanelTitleLabel.Text = _subFeaturePanelVm.Title;
        }

        PanelOverlay.HeightRequest = EquipInvGrid.Height > 0 ? EquipInvGrid.Height : -1;
        if (slideIn)
        {
            PanelOverlay.TranslationY = Height;      // start below the bottom of the window
            PanelOverlay.IsVisible = true;
        }
        _openPanel = panel;

        if (slideIn)
        {
            await PanelOverlay.TranslateToAsync(0, 0, 380, Easing.CubicOut);
            SetBackgroundPanelsCovered(true);   // fully covered now: hide what's underneath
        }
    }

    private async Task HidePanelAsync()
    {
        await SaveScratchPadAsync();   // any panel closing saves the pad, so nothing typed is lost
        _viewModel.RefreshFamiliarStrip();   // changes made inside a panel (Familiars, Crafting) show on the dashboard
        SetBackgroundPanelsCovered(false);   // bring the equipment and inventory back before the slide-out starts
        await PanelOverlay.TranslateToAsync(0, Height, 300, Easing.CubicIn);
        PanelOverlay.IsVisible = false;
        _openPanel = OverlayPanel.None;
    }

    // The overlay has no solid fill when no skin is loaded, so the equipment and inventory panels
    // would show through it and swallow taps. Hide them while a panel covers them. Opacity (not
    // IsVisible) keeps the row's size, so the overlay height and the layout never change.
    private void SetBackgroundPanelsCovered(bool covered)
    {
        EquipInvGrid.Opacity = covered ? 0 : 1;
        EquipInvGrid.InputTransparent = covered;
    }

    private async void OnBestowFamiliarRequested()
    {
        var familiarData = IPlatformApplication.Current!.Services.GetRequiredService<IFamiliarDataService>();

        // Keep the panel open and skip the reload/reset in OnAppearing when the modal closes.
        SetReturningFromFlyout(true);

        var page = new BestowFamiliarPage(familiarData, _viewModel.CharDataService, () =>
        {
            _familiarPanelVm?.Refresh();
            _familiarPanelVm?.SelectLast();
        });
        await Navigation.PushModalAsync(page);
    }

    private async void OnBestowSubFeatureRequested(SubFeatureType type)
    {
        if (_liveSubFeatures == null) return;

        // Keep the panel open and skip the reload/reset in OnAppearing when the modal closes.
        SetReturningFromFlyout(true);

        var page = new BestowSubFeaturePage(_liveSubFeatures, type, () => _subFeaturePanelVm?.Refresh());
        await Navigation.PushModalAsync(page);
    }

    private async void OnCastRequested(SubFeatureEntry entry)
    {
        if (_liveSubFeatures == null || _magicCasting == null) return;

        // Keep the panel open and skip the reload/reset in OnAppearing when the modal closes.
        SetReturningFromFlyout(true);

        var techniques = _liveSubFeatures.Owned(SubFeatureType.Technique);
        var page = entry.Type == SubFeatureType.Spell
            ? new MagicCastPage(_magicCasting, entry, null, techniques, OnCastFinished)
            : new MagicCastPage(_magicCasting, null, entry, techniques, OnCastFinished);
        await Navigation.PushModalAsync(page);
    }

    private async void OnCastFinished(string summary)
    {
        _subFeaturePanelVm?.ShowResult(summary);
        await _viewModel.LoadCharacterDataAsync();   // fatigue, Hit Protection and Mind may have changed
    }

    private async void OnFeatureUseRequested(FeatureEntry entry)
    {
        if (_liveFeatures == null) return;

        // The Shifter's transformation: transform, or manage the form that's already up.
        if (entry.IsShapeshift)
        {
            if (_viewModel.Shifter.IsTransformed)
            {
                await _viewModel.ManageTransformationAsync();
                return;
            }

            SetReturningFromFlyout(true);
            await Navigation.PushModalAsync(new ShapeshiftPage(_viewModel.Shifter, entry.Class, OnFeatureUsed));
            return;
        }

        // Keep the panel open and skip the reload/reset in OnAppearing when the modal closes.
        SetReturningFromFlyout(true);

        var page = new FeatureUsePage(_liveFeatures, entry, OnFeatureUsed);
        await Navigation.PushModalAsync(page);
    }

    private async void OnFeatureUsed(string summary)
    {
        _featurePanelVm?.ShowResult(summary);
        if (_featurePanelVm != null) await _featurePanelVm.RefreshAsync();
        await _viewModel.LoadCharacterDataAsync();   // fatigue, Spirit, chips and Favour may have changed
    }

    private async void OnCraftRequested(SubFeatureEntry? blueprint)
    {
        if (_craftService == null) return;

        // Keep the panel open and skip the reload/reset in OnAppearing when the modal closes.
        SetReturningFromFlyout(true);

        var page = new CraftPage(_craftService, blueprint, OnCraftFinished);
        await Navigation.PushModalAsync(page);
    }

    private async void OnCraftFinished(string summary)
    {
        _subFeaturePanelVm?.ShowResult(summary);
        await _viewModel.LoadCharacterDataAsync();   // the pack changed
    }
}