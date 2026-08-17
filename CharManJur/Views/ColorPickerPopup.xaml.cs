namespace CharManJur.Views;

public partial class ColorPickerPopup : ContentPage
{
    private readonly Action<Color> _onApply;
    private readonly Services.IThemeService? _themeService;

    public ColorPickerPopup(string displayName, Color initialColor, Action<Color> onApply)
    {
        InitializeComponent();
        _onApply = onApply;
        _themeService = Application.Current?.Handler?.MauiContext?.Services?.GetService<Services.IThemeService>();

        lblColorName.Text = displayName;

        sliderR.Value = initialColor.Red * 255;
        sliderG.Value = initialColor.Green * 255;
        sliderB.Value = initialColor.Blue * 255;

        UpdatePreview();
        UpdatePasteButtonState();
    }

    private void OnSliderChanged(object sender, ValueChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        int r = (int)sliderR.Value;
        int g = (int)sliderG.Value;
        int b = (int)sliderB.Value;

        var color = Color.FromRgb(r, g, b);

        previewSwatch.Color = color;
        lblHexPreview.Text = color.ToArgbHex();
        lblR.Text = r.ToString();
        lblG.Text = g.ToString();
        lblB.Text = b.ToString();
    }

    private void UpdatePasteButtonState()
    {
        btnPaste.IsEnabled = _themeService?.ClipboardColor != null;
    }

    private void OnCopyClicked(object sender, EventArgs e)
    {
        if (_themeService == null) return;

        int r = (int)sliderR.Value;
        int g = (int)sliderG.Value;
        int b = (int)sliderB.Value;

        _themeService.ClipboardColor = Color.FromRgb(r, g, b);
        UpdatePasteButtonState();
    }

    private void OnPasteClicked(object sender, EventArgs e)
    {
        var clipboard = _themeService?.ClipboardColor;
        if (clipboard == null) return;

        sliderR.Value = clipboard.Red * 255;
        sliderG.Value = clipboard.Green * 255;
        sliderB.Value = clipboard.Blue * 255;

        UpdatePreview();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnApplyClicked(object sender, EventArgs e)
    {
        int r = (int)sliderR.Value;
        int g = (int)sliderG.Value;
        int b = (int)sliderB.Value;

        _onApply(Color.FromRgb(r, g, b));
        await Navigation.PopModalAsync();
    }
}