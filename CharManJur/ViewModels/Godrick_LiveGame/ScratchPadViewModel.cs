using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Helpers;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class ScratchPageRow
{
    public ScratchPageInfo Info { get; init; } = null!;
    public string Name => Info.Name;
    public string Icon => Info.Type == ScratchPageType.Drawing ? "🖊️" : "📝";
    public bool IsSelected { get; init; }
    public ICommand SelectCommand { get; init; } = null!;
}

// The Scratch Pad panel: a page list, and the open page. Edits are kept in memory and written when the pad is
// closed, a page is switched, or the player stops typing for a few seconds.
public class ScratchPadViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan IdleSave = TimeSpan.FromSeconds(8);

    private ScratchPadStore? _store;
    private Guid _characterGuid;
    private ScratchIndex _index = new();
    private ScratchPageInfo? _current;
    private ScratchPageData? _data;
    private string _text = string.Empty;
    private bool _dirty;
    private bool _loading;
    private bool _editing;
    private CancellationTokenSource? _idleSave;
    private bool _showTools;

    // One board for the canvas and the tools beside it.
    public DrawingBoard Board { get; } = new();

    public ObservableCollection<ScratchPageRow> PageRows { get; } = new();

    public ICommand NewTextCommand { get; }
    public ICommand NewDrawingCommand { get; }
    public ICommand ToggleToolsCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ToggleModeCommand { get; }
    public ICommand SetBackgroundCommand { get; }
    public ICommand RemoveBackgroundCommand { get; }

    public ScratchPadViewModel()
    {
        NewTextCommand = new Command(async () => await CreateTextAsync());
        NewDrawingCommand = new Command(async () => await CreateDrawingAsync());
        ToggleToolsCommand = new Command(() => { _showTools = !_showTools; Notify(); });
        RenameCommand = new Command(async () => await RenameAsync());
        DeleteCommand = new Command(async () => await DeleteAsync());
        MoveUpCommand = new Command(async () => await MoveAsync(-1));
        MoveDownCommand = new Command(async () => await MoveAsync(+1));
        ToggleModeCommand = new Command(async () => await ToggleModeAsync());
        SetBackgroundCommand = new Command(async () => await SetBackgroundAsync());
        RemoveBackgroundCommand = new Command(async () => await RemoveBackgroundAsync());
    }

    // ===== STATE FOR THE VIEW =====
    public bool HasPages => PageRows.Count > 0;
    public bool HasSelection => _current != null;
    public bool HasNoSelection => _current == null;
    public string SelectedName => _current?.Name ?? string.Empty;
    public bool IsTextPage => _current?.Type == ScratchPageType.Text;
    public bool IsDrawingPage => _current?.Type == ScratchPageType.Drawing;
    public bool IsEditing => IsTextPage && _editing;
    public bool IsReading => IsTextPage && !_editing;
    public List<DrawShape>? DrawingShapes => IsDrawingPage ? _data?.Shapes : null;
    public string? BackgroundImageName => IsDrawingPage ? _data?.BackgroundImage : null;
    public bool HasBackground => BackgroundImageName != null;
    public IImage? LoadBackgroundImage() => BackgroundImageName is { } name ? _store?.LoadBackground(name) : null;
    // A drawing page shows its tools where the page list normally is; the toggle swaps them.
    public bool ShowTools => IsDrawingPage && _showTools;
    public bool ShowPageList => !ShowTools;
    public string ToolsToggleText => _showTools ? "📄 Pages" : "🎨 Tools";
    public string ModeButtonText => _editing ? "📖 Read" : "✏️ Edit";

    private int CurrentIndex => _current == null ? -1 : _index.Pages.FindIndex(p => p.Id == _current.Id);
    public bool CanMoveUp => CurrentIndex > 0;
    public bool CanMoveDown => CurrentIndex >= 0 && CurrentIndex < _index.Pages.Count - 1;

    // The text of the open page. Typing marks it for saving.
    public string Text
    {
        get => _text;
        set
        {
            value ??= string.Empty;
            if (_text == value) return;
            _text = value;
            if (_loading) return;
            _dirty = true;
            ScheduleIdleSave();
        }
    }

    // ===== OPEN / SAVE =====
    // Called whenever the panel opens (and for a different character, it starts fresh).
    public Task OpenAsync(Guid characterGuid) => OpenAsync(characterGuid, () => new ScratchPadStore(characterGuid));

    // For a pad that isn't a character's (the GM's): key says which pad this is, and createStore makes it when the key changes.
    public async Task OpenAsync(Guid key, Func<ScratchPadStore> createStore)
    {
        await SaveAsync();

        if (_store == null || _characterGuid != key)
        {
            _characterGuid = key;
            _store = createStore();
            _current = null;
            _data = null;
        }

        _index = await _store.LoadIndexAsync();
        var keep = _current == null ? null : _index.Pages.FirstOrDefault(p => p.Id == _current.Id);
        await ShowPageAsync(keep ?? _index.Pages.FirstOrDefault());
    }

    public async Task SaveAsync()
    {
        _idleSave?.Cancel();
        if (!_dirty || _store == null || _data == null) return;

        _dirty = false;
        var page = _data;
        page.Text = _text;
        try
        {
            await _store.SavePageAsync(page);
        }
        catch (Exception ex)
        {
            _dirty = true;   // try again at the next chance
            System.Diagnostics.Debug.WriteLine($"[SCRATCH] Save failed: {ex.Message}");
        }
    }

    private void ScheduleIdleSave()
    {
        _idleSave?.Cancel();
        _idleSave = new CancellationTokenSource();
        var token = _idleSave.Token;
        _ = Task.Delay(IdleSave, token).ContinueWith(
            _ => MainThread.BeginInvokeOnMainThread(async () => await SaveAsync()),
            token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    // ===== PAGES =====
    private async Task ShowPageAsync(ScratchPageInfo? info)
    {
        _current = info;
        _data = null;

        _loading = true;
        if (info != null && _store != null)
        {
            _data = await _store.LoadPageAsync(info.Id) ?? new ScratchPageData { Id = info.Id, Type = info.Type };
            _text = _data.Text;
        }
        else
        {
            _text = string.Empty;
        }
        _loading = false;
        _dirty = false;

        _editing = info is { Type: ScratchPageType.Text } && _text.Length == 0;   // a new text page opens ready to type; a written one opens to read
        _showTools = info is { Type: ScratchPageType.Drawing };
        RebuildRows();
        Notify();
    }

    private async Task SelectAsync(ScratchPageInfo info)
    {
        if (_current?.Id == info.Id) return;
        await SaveAsync();
        await ShowPageAsync(info);
    }

    private async Task CreateTextAsync()
    {
        if (_store == null) return;
        await SaveAsync();

        int number = _index.Pages.Count(p => p.Type == ScratchPageType.Text) + 1;
        var info = new ScratchPageInfo { Name = $"Text Page {number}", Type = ScratchPageType.Text };

        _index.Pages.Add(info);
        await _store.SavePageAsync(new ScratchPageData { Id = info.Id, Type = info.Type });
        await _store.SaveIndexAsync(_index);
        await ShowPageAsync(info);
    }

    private async Task CreateDrawingAsync()
    {
        if (_store == null) return;
        await SaveAsync();

        int number = _index.Pages.Count(p => p.Type == ScratchPageType.Drawing) + 1;
        var info = new ScratchPageInfo { Name = $"Drawing {number}", Type = ScratchPageType.Drawing };

        _index.Pages.Add(info);
        await _store.SavePageAsync(new ScratchPageData { Id = info.Id, Type = info.Type });
        await _store.SaveIndexAsync(_index);
        await ShowPageAsync(info);
    }

    // ===== BACKGROUND IMAGE =====
    private async Task SetBackgroundAsync()
    {
        if (_store == null || _data == null || !IsDrawingPage) return;

        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a map or picture",
                FileTypes = FilePickerFileType.Images
            });
            if (picked == null) return;

            string? name;
            using (var stream = await picked.OpenReadAsync())
                name = await _store.ImportBackgroundAsync(_data.Id, stream, _data.BackgroundImage);
            if (name == null) return;

            _data.BackgroundImage = name;
            _dirty = true;
            await SaveAsync();
            Notify();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SCRATCH] Background failed: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Background", "That image couldn't be used. Try a PNG or JPG.", "OK");
        }
    }

    private async Task RemoveBackgroundAsync()
    {
        if (_store == null || _data?.BackgroundImage == null) return;

        _store.DeleteBackground(_data.BackgroundImage);
        _data.BackgroundImage = null;
        _dirty = true;
        await SaveAsync();
        Notify();
    }

    // The drawing board changed the page's shapes: save them soon.
    public void MarkDrawingChanged()
    {
        if (_loading || _data == null) return;
        _dirty = true;
        ScheduleIdleSave();
    }

    private async Task RenameAsync()
    {
        if (_current == null || _store == null) return;

        string? name = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Rename Page", "Page name:", "Rename", "Cancel", initialValue: _current.Name, maxLength: 40);
        if (string.IsNullOrWhiteSpace(name)) return;

        _current.Name = name.Trim();
        await _store.SaveIndexAsync(_index);
        RebuildRows();
        Notify();
    }

    private async Task DeleteAsync()
    {
        if (_current == null || _store == null) return;

        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Page", $"Delete \"{_current.Name}\"? This can't be undone.", "Delete", "Cancel");
        if (!ok) return;

        _idleSave?.Cancel();
        _dirty = false;

        int at = CurrentIndex;
        var gone = _current;
        _index.Pages.RemoveAll(p => p.Id == gone.Id);
        await _store.SaveIndexAsync(_index);
        await _store.DeletePageAsync(gone.Id);

        await ShowPageAsync(_index.Pages.Count == 0 ? null : _index.Pages[Math.Min(Math.Max(at, 0), _index.Pages.Count - 1)]);
    }

    private async Task MoveAsync(int delta)
    {
        if (_current == null || _store == null) return;

        int from = CurrentIndex;
        int to = from + delta;
        if (from < 0 || to < 0 || to >= _index.Pages.Count) return;

        (_index.Pages[from], _index.Pages[to]) = (_index.Pages[to], _index.Pages[from]);
        await _store.SaveIndexAsync(_index);
        RebuildRows();
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
    }

    private async Task ToggleModeAsync()
    {
        _editing = !_editing;
        if (!_editing) await SaveAsync();   // reading is a natural stopping point
        Notify();
    }

    // Tapping the read view starts editing. Does nothing if already editing.
    public void BeginEdit()
    {
        if (!IsTextPage || _editing) return;
        _editing = true;
        Notify();
    }

    private void RebuildRows()
    {
        PageRows.Clear();
        foreach (var page in _index.Pages)
        {
            var info = page;
            PageRows.Add(new ScratchPageRow
            {
                Info = info,
                IsSelected = _current?.Id == info.Id,
                SelectCommand = new Command(async () => await SelectAsync(info))
            });
        }
    }

    // Everything changed: the view refreshes all bindings and re-renders the read view.
    private void Notify() => OnPropertyChanged(string.Empty);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}