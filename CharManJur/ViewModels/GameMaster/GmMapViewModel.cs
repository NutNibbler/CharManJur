using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Helpers;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// One symbol in the library, as a tile.
public sealed class MapSymbolRow
{
    public MapSymbolRow(SymbolDef def, MapStore store, Action<SymbolDef> place, Action<SymbolDef> remove)
    {
        Def = def;
        IsImage = def.Kind == SymbolKind.Image;
        IsToken = !IsImage;

        if (IsImage) Preview = ImageSource.FromStream(() => store.OpenSymbol(def) ?? new MemoryStream());

        TokenBackground = MapTokenColors.Get(def.Color);
        TokenTextColor = MapTokenColors.TextOn(def.Color);
        TokenText = def.Letter ?? string.Empty;

        PlaceCommand = new Command(() => place(def));
        DeleteCommand = new Command(() => remove(def));
    }

    public SymbolDef Def { get; }
    public string Name => Def.Name;
    public bool IsImage { get; }
    public bool IsToken { get; }
    public ImageSource? Preview { get; }
    public Color TokenBackground { get; }
    public Color TokenTextColor { get; }
    public string TokenText { get; }
    public ICommand PlaceCommand { get; }
    public ICommand DeleteCommand { get; }
}

// The GM's map tracker: the campaign's maps, the open map's picture, drawing and symbols, and the symbol library.
// Edits are kept in memory and written when the map is switched, the dashboard is left, or after a few idle seconds.
public class GmMapViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan IdleSave = TimeSpan.FromSeconds(8);
    private const float RotateStep = 15f;

    private readonly ICampaignStore _campaigns;

    private MapStore? _store;
    private Guid _campaignId;
    private MapCatalog _index = new();
    private MapInfo? _current;
    private MapData? _data;
    private SymbolLibrary _library = new();
    private List<string> _mapNames = new();
    private bool _dirty;
    private bool _loading;
    private CancellationTokenSource? _idleSave;

    public GmMapViewModel(ICampaignStore campaigns)
    {
        _campaigns = campaigns;

        Board.Changed += MarkChanged;
        Board.SelectionChanged += () => OnPropertyChanged(nameof(HasSelectedSymbol));

        NewMapCommand = new Command(async () => await NewMapAsync());
        RenameMapCommand = new Command(async () => await RenameMapAsync());
        DeleteMapCommand = new Command(async () => await DeleteMapAsync());
        SetImageCommand = new Command(async () => await SetImageAsync());
        RemoveImageCommand = new Command(async () => await RemoveImageAsync());
        ImportSymbolsCommand = new Command(async () => await ImportSymbolsAsync());
        NewTokenCommand = new Command(async () => await NewTokenAsync());

        RotateLeftCommand = new Command(() => Board.RotateSelected(-RotateStep));
        RotateRightCommand = new Command(() => Board.RotateSelected(RotateStep));
        SmallerCommand = new Command(() => Board.ScaleSelected(1 / 1.15f));
        BiggerCommand = new Command(() => Board.ScaleSelected(1.15f));
        FrontCommand = new Command(() => Board.BringSelectedToFront());
        DuplicateCommand = new Command(() => Board.DuplicateSelected());
        DeleteSymbolCommand = new Command(() => Board.DeleteSelected());
    }

    // One board for the surface and the tools beside it.
    public MapBoard Board { get; } = new();

    public ObservableCollection<MapSymbolRow> SymbolRows { get; } = new();

    public ICommand NewMapCommand { get; }
    public ICommand RenameMapCommand { get; }
    public ICommand DeleteMapCommand { get; }
    public ICommand SetImageCommand { get; }
    public ICommand RemoveImageCommand { get; }
    public ICommand ImportSymbolsCommand { get; }
    public ICommand NewTokenCommand { get; }
    public ICommand RotateLeftCommand { get; }
    public ICommand RotateRightCommand { get; }
    public ICommand SmallerCommand { get; }
    public ICommand BiggerCommand { get; }
    public ICommand FrontCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand DeleteSymbolCommand { get; }

    // ===== STATE FOR THE VIEW =====
    public bool HasMap => _current != null;
    public bool HasNoMap => _current == null;
    public bool HasBackground => _data?.ImageFile != null;
    public bool HasSelectedSymbol => Board.Selected != null;
    public bool HasNoSymbols => SymbolRows.Count == 0;

    public List<string> MapNames => _mapNames;

    public int SelectedMapIndex
    {
        get => _current == null ? -1 : _index.Maps.FindIndex(m => m.Id == _current.Id);
        set
        {
            if (value < 0 || value >= _index.Maps.Count) return;   // the picker reports -1 while its list is rebuilt
            var info = _index.Maps[value];
            if (_current?.Id == info.Id) return;
            _ = SelectMapAsync(info);
        }
    }

    // ===== OPEN / SAVE =====
    // Called when a session loads (and for a different campaign, it starts fresh).
    public async Task OpenAsync(Guid campaignId)
    {
        await SaveAsync();

        if (_store == null || _campaignId != campaignId)
        {
            _campaignId = campaignId;
            _store = new MapStore(_campaigns.MapsFolder(campaignId), _campaigns.SymbolsFolder(campaignId));
            _current = null;
            _data = null;
        }

        _index = await _store.LoadIndexAsync();
        _library = await _store.LoadLibraryAsync();
        RebuildSymbols();

        var keep = _current == null ? null : _index.Maps.FirstOrDefault(m => m.Id == _current.Id);
        await ShowMapAsync(keep ?? _index.Maps.FirstOrDefault());
    }

    public async Task SaveAsync()
    {
        _idleSave?.Cancel();
        if (!_dirty || _store == null || _data == null) return;

        _dirty = false;
        try
        {
            await _store.SaveMapAsync(_data);
        }
        catch (Exception ex)
        {
            _dirty = true;   // try again at the next chance
            System.Diagnostics.Debug.WriteLine($"[MAP] Save failed: {ex.Message}");
        }
    }

    // The board changed the map's shapes or symbols: save them soon.
    private void MarkChanged()
    {
        if (_loading || _data == null) return;
        _dirty = true;

        _idleSave?.Cancel();
        _idleSave = new CancellationTokenSource();
        var token = _idleSave.Token;
        _ = Task.Delay(IdleSave, token).ContinueWith(
            _ => MainThread.BeginInvokeOnMainThread(async () => await SaveAsync()),
            token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    // ===== MAPS =====
    private async Task ShowMapAsync(MapInfo? info)
    {
        _current = info;
        _data = null;

        _loading = true;
        if (info != null && _store != null)
        {
            _data = await _store.LoadMapAsync(info.Id) ?? new MapData { Id = info.Id };
            Board.Load(_data);
            Board.SetBackground(_data.ImageFile == null ? null : _store.LoadImage(info.Id, _data.ImageFile));
        }
        else
        {
            Board.Load(new MapData());
            Board.SetBackground(null);
        }
        _loading = false;
        _dirty = false;

        RebuildMapNames();
        RaiseMapState();
    }

    private async Task SelectMapAsync(MapInfo info)
    {
        await SaveAsync();
        await ShowMapAsync(info);
    }

    private async Task NewMapAsync()
    {
        if (_store == null) return;

        string? name = await Application.Current!.MainPage!.DisplayPromptAsync(
            "New Map", "Name this map (World, City, Dungeon...):", "Create", "Cancel",
            initialValue: $"Map {_index.Maps.Count + 1}", maxLength: 40);
        if (string.IsNullOrWhiteSpace(name)) return;

        await SaveAsync();

        var info = new MapInfo { Name = name.Trim() };
        _index.Maps.Add(info);
        await _store.SaveMapAsync(new MapData { Id = info.Id });
        await _store.SaveIndexAsync(_index);
        await ShowMapAsync(info);
    }

    private async Task RenameMapAsync()
    {
        if (_current == null || _store == null) return;

        string? name = await Application.Current!.MainPage!.DisplayPromptAsync(
            "Rename Map", "Map name:", "Rename", "Cancel", initialValue: _current.Name, maxLength: 40);
        if (string.IsNullOrWhiteSpace(name)) return;

        _current.Name = name.Trim();
        await _store.SaveIndexAsync(_index);
        RebuildMapNames();
    }

    private async Task DeleteMapAsync()
    {
        if (_current == null || _store == null) return;

        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Delete Map", $"Delete \"{_current.Name}\", its drawing, its symbols and its picture? This can't be undone.", "Delete", "Cancel");
        if (!ok) return;

        _idleSave?.Cancel();
        _dirty = false;

        int at = SelectedMapIndex;
        var gone = _current;
        _index.Maps.RemoveAll(m => m.Id == gone.Id);
        await _store.SaveIndexAsync(_index);
        await _store.DeleteMapAsync(gone.Id);

        await ShowMapAsync(_index.Maps.Count == 0 ? null : _index.Maps[Math.Min(Math.Max(at, 0), _index.Maps.Count - 1)]);
    }

    private void RebuildMapNames()
    {
        _mapNames = _index.Maps.Select(m => m.Name).ToList();
        OnPropertyChanged(nameof(MapNames));
        OnPropertyChanged(nameof(SelectedMapIndex));   // puts the selection back after the list changes
    }

    private void RaiseMapState()
    {
        OnPropertyChanged(nameof(HasMap));
        OnPropertyChanged(nameof(HasNoMap));
        OnPropertyChanged(nameof(HasBackground));
        OnPropertyChanged(nameof(HasSelectedSymbol));
    }

    // ===== MAP PICTURE =====
    private async Task SetImageAsync()
    {
        if (_store == null || _data == null || _current == null) return;

        try
        {
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a map picture (PNG or JPG)",
                FileTypes = FilePickerFileType.Images
            });
            if (picked == null) return;

            MapImportResult result;
            using (var stream = await picked.OpenReadAsync())
                result = await _store.ImportImageAsync(_data, stream, Path.GetExtension(picked.FileName));

            var page = Application.Current!.MainPage!;
            if (!result.Success)
            {
                await page.DisplayAlertAsync("Map Picture", result.Message, "OK");
                return;
            }

            await _store.SaveMapAsync(_data);
            Board.SetBackground(_data.ImageFile == null ? null : _store.LoadImage(_data.Id, _data.ImageFile));
            OnPropertyChanged(nameof(HasBackground));

            if (result.Resized)
                await page.DisplayAlertAsync(
                    "Large Picture",
                    $"The picture is {result.OriginalWidth} × {result.OriginalHeight}. It's shown as a 3000 pixel copy; your original file is kept as it was.",
                    "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Picture failed: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlertAsync("Map Picture", "That picture couldn't be used. Try a PNG or JPG.", "OK");
        }
    }

    private async Task RemoveImageAsync()
    {
        if (_store == null || _data?.ImageFile == null) return;

        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Remove Picture", "Remove this map's picture? The original file is deleted too. Your drawing and symbols stay.", "Remove", "Cancel");
        if (!ok) return;

        await _store.RemoveImageAsync(_data);
        await _store.SaveMapAsync(_data);
        Board.SetBackground(null);
        OnPropertyChanged(nameof(HasBackground));
    }

    // ===== SYMBOL LIBRARY =====
    private void RebuildSymbols()
    {
        if (_store == null) return;
        var store = _store;

        SymbolRows.Clear();
        foreach (var def in _library.Symbols)
            SymbolRows.Add(new MapSymbolRow(def, store, PlaceSymbol, d => _ = RemoveSymbolAsync(d)));

        Board.SetSymbols(_library.Symbols, d => store.LoadSymbolImage(d));
        OnPropertyChanged(nameof(HasNoSymbols));
    }

    private void PlaceSymbol(SymbolDef def)
    {
        if (_current == null)
        {
            _ = Application.Current!.MainPage!.DisplayAlertAsync("Symbols", "Create a map first, then place symbols on it.", "OK");
            return;
        }
        Board.PlaceSymbolAtCenter(def.Id);
    }

    private async Task ImportSymbolsAsync()
    {
        if (_store == null) return;

        try
        {
            var picks = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Choose symbol pictures (PNG or JPG)",
                FileTypes = FilePickerFileType.Images
            });
            if (picks == null) return;

            int failed = 0;
            foreach (var pick in picks)
            {
                using var stream = await pick.OpenReadAsync();
                var def = await _store.ImportSymbolAsync(stream, Path.GetFileNameWithoutExtension(pick.FileName));
                if (def == null) failed++;
            }

            _library = await _store.LoadLibraryAsync();
            RebuildSymbols();

            if (failed > 0)
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Symbols", $"{failed} picture(s) couldn't be read. Try PNG or JPG files.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Symbol import failed: {ex.Message}");
        }
    }

    private async Task NewTokenAsync()
    {
        if (_store == null) return;
        var page = Application.Current!.MainPage!;

        string? letter = await page.DisplayPromptAsync(
            "New Token", "A letter or number for the token (up to 3 characters, or leave it empty):",
            "Next", "Cancel", maxLength: 3);
        if (letter == null) return;

        string? color = await page.DisplayActionSheetAsync("Token color", "Cancel", null, MapTokenColors.Names);
        if (string.IsNullOrEmpty(color) || color == "Cancel") return;

        await _store.AddTokenAsync(letter, color);
        _library = await _store.LoadLibraryAsync();
        RebuildSymbols();
    }

    private async Task RemoveSymbolAsync(SymbolDef def)
    {
        if (_store == null) return;

        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Remove Symbol",
            $"Remove \"{def.Name}\" from the library? Maps that use it show a ? in its place.", "Remove", "Cancel");
        if (!ok) return;

        await _store.DeleteSymbolAsync(def);
        _library = await _store.LoadLibraryAsync();
        RebuildSymbols();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}