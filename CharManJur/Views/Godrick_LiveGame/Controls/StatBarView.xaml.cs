namespace CharManJur.Views.Godrick_LiveGame.Controls;

public partial class StatBarView : ContentView
{
    public static readonly BindableProperty StatLabelProperty =
        BindableProperty.Create(nameof(StatLabel), typeof(string), typeof(StatBarView), string.Empty);

    public static readonly BindableProperty CurrentValueProperty =
        BindableProperty.Create(nameof(CurrentValue), typeof(int), typeof(StatBarView), 0, propertyChanged: OnValuesChanged);

    public static readonly BindableProperty MaxValueProperty =
        BindableProperty.Create(nameof(MaxValue), typeof(int), typeof(StatBarView), 1, propertyChanged: OnValuesChanged);

    public static readonly BindableProperty BarColorProperty =
        BindableProperty.Create(nameof(BarColor), typeof(Color), typeof(StatBarView), Colors.White);

    public string StatLabel
    {
        get => (string)GetValue(StatLabelProperty);
        set => SetValue(StatLabelProperty, value);
    }

    public static readonly BindableProperty BadgeProperty =
    BindableProperty.Create(nameof(Badge), typeof(string), typeof(StatBarView), string.Empty);

    public string Badge
    {
        get => (string)GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    public int CurrentValue
    {
        get => (int)GetValue(CurrentValueProperty);
        set => SetValue(CurrentValueProperty, value);
    }

    public int MaxValue
    {
        get => (int)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    public static readonly BindableProperty BarWidthProperty =
        BindableProperty.Create(nameof(BarWidth), typeof(double), typeof(StatBarView), 150.0);

    public static readonly BindableProperty MaxValueColorProperty =
        BindableProperty.Create(nameof(MaxValueColor), typeof(Color), typeof(StatBarView), Colors.White);

    public Color MaxValueColor
    {
        get => (Color)GetValue(MaxValueColorProperty);
        set => SetValue(MaxValueColorProperty, value);
    }

    public double BarWidth
    {
        get => (double)GetValue(BarWidthProperty);
        set => SetValue(BarWidthProperty, value);
    }

    public string ValueText => $"{CurrentValue} / {MaxValue}";
    public double FillRatio => MaxValue <= 0 ? 0 : Math.Clamp((double)CurrentValue / MaxValue, 0, 1);

    public StatBarView()
    {
        InitializeComponent();
    }

    private static void OnValuesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (StatBarView)bindable;
        view.OnPropertyChanged(nameof(ValueText));
        view.OnPropertyChanged(nameof(FillRatio));
    }
}