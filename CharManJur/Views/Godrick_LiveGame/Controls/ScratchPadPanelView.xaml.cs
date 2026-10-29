using CharManJur.Helpers;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame.Controls;

public partial class ScratchPadPanelView : ContentView
{
    private ScratchPadViewModel? _vm;

    public ScratchPadPanelView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
        _vm = BindingContext as ScratchPadViewModel;
        if (_vm != null) _vm.PropertyChanged += OnVmChanged;
    }

    // The view model raises "everything changed" when a page opens or the mode switches: redraw the read view.
    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) MainThread.BeginInvokeOnMainThread(RenderRead);
    }

    private void RenderRead()
    {
        ReadHost.Children.Clear();
        if (_vm == null || !_vm.IsReading) return;

        if (string.IsNullOrWhiteSpace(_vm.Text))
        {
            ReadHost.Children.Add(new Label { Text = "This page is empty. Click here to write on it.", FontSize = 12, TextColor = Colors.Gray });
            return;
        }

        foreach (var view in ScratchMarkup.Render(_vm.Text)) ReadHost.Children.Add(view);
    }

    // Click the text to start editing, with the cursor at the end.
    private void OnReadTapped(object? sender, TappedEventArgs e)
    {
        if (_vm == null) return;
        _vm.BeginEdit();

        Dispatcher.Dispatch(() =>
        {
            TextEditor.Focus();
            TextEditor.CursorPosition = (TextEditor.Text ?? string.Empty).Length;
        });
    }

    // ===== FORMATTING BUTTONS =====
    private void OnBoldClicked(object? sender, EventArgs e) => Wrap("**");
    private void OnItalicClicked(object? sender, EventArgs e) => Wrap("*");

    // Wraps the selection in the marker, or drops a pair of markers at the cursor with the cursor between them.
    private void Wrap(string marker)
    {
        string text = TextEditor.Text ?? string.Empty;
        int start = Math.Clamp(TextEditor.CursorPosition, 0, text.Length);
        int length = Math.Clamp(TextEditor.SelectionLength, 0, text.Length - start);

        if (length > 0)
        {
            TextEditor.Text = text.Insert(start + length, marker).Insert(start, marker);
            TextEditor.CursorPosition = start + length + marker.Length * 2;
        }
        else
        {
            TextEditor.Text = text.Insert(start, marker + marker);
            TextEditor.CursorPosition = start + marker.Length;
        }

        TextEditor.SelectionLength = 0;
        TextEditor.Focus();
    }
}