namespace CharManJur.Views.Godrick_LiveGame.Controls;

// How many uses a new item starts with: 0 up to its maximum. The page sets MaximumUses and Uses (full) when an
// item is chosen; a binding can read Uses back.
public partial class StartingUsesView : ContentView
{
    public static readonly BindableProperty MaximumUsesProperty = BindableProperty.Create(
        nameof(MaximumUses), typeof(int), typeof(StartingUsesView), 1,
        propertyChanged: (b, _, _) => ((StartingUsesView)b).OnMaximumChanged());

    public static readonly BindableProperty UsesProperty = BindableProperty.Create(
        nameof(Uses), typeof(int), typeof(StartingUsesView), 1, BindingMode.TwoWay,
        coerceValue: (b, v) => Math.Clamp((int)v, 0, Math.Max(0, ((StartingUsesView)b).MaximumUses)),
        propertyChanged: (b, _, _) => ((StartingUsesView)b).ShowValue());

    public int MaximumUses
    {
        get => (int)GetValue(MaximumUsesProperty);
        set => SetValue(MaximumUsesProperty, value);
    }

    public int Uses
    {
        get => (int)GetValue(UsesProperty);
        set => SetValue(UsesProperty, value);
    }

    public StartingUsesView()
    {
        InitializeComponent();
        ShowValue();
    }

    private void OnMaximumChanged()
    {
        CoerceValue(UsesProperty);   // a lower maximum pulls the value down with it
        ShowValue();
    }

    private void ShowValue()
    {
        MaxLabel.Text = $"of {MaximumUses}";

        // Don't rewrite the box while the player is typing a value that is already right.
        string text = Uses.ToString();
        if (UsesEntry.Text != text && !(int.TryParse(UsesEntry.Text, out int typed) && typed == Uses))
            UsesEntry.Text = text;
    }

    private void OnDownClicked(object? sender, EventArgs e) => Uses -= 1;
    private void OnUpClicked(object? sender, EventArgs e) => Uses += 1;
    private void OnEmptyClicked(object? sender, EventArgs e) => Uses = 0;
    private void OnFullClicked(object? sender, EventArgs e) => Uses = MaximumUses;

    private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (int.TryParse(e.NewTextValue, out int typed)) Uses = typed;   // clamped into 0..maximum
    }
}