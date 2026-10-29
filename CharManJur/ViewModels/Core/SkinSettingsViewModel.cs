using CharManJur.Models;
using CharManJur.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CharManJur.ViewModels;

// One row of the load-order list
public class SkinPackEntry : INotifyPropertyChanged
{
    private readonly Action _onEnabledChanged;
    private bool _isEnabled;
    private int _position = 1;
    private bool _isFirst = true;
    private bool _isLast = true;

    public SkinPackEntry(SkinPack pack, bool isEnabled, Action onEnabledChanged)
    {
        Pack = pack;
        _isEnabled = isEnabled;
        _onEnabledChanged = onEnabledChanged;
    }

    public SkinPack Pack { get; }
    public string Folder => Pack.FolderName;
    public string Name => Pack.DisplayName;
    public string Description => Pack.Description;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Pack.Description);
    public string Warning => Pack.Warning ?? string.Empty;
    public bool HasWarning => !string.IsNullOrEmpty(Pack.Warning);

    public string Subtitle
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pack.Author)) parts.Add($"by {Pack.Author}");
            if (!string.IsNullOrWhiteSpace(Pack.Version)) parts.Add($"v{Pack.Version}");
            parts.Add($"{Pack.Images.Count} image(s)");
            if (Pack.Fonts.Count > 0) parts.Add($"{Pack.Fonts.Count} font(s)");
            return string.Join(" • ", parts);
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            OnPropertyChanged();
            _onEnabledChanged();
        }
    }

    public string PriorityText => _position.ToString();
    public bool CanMoveUp => !_isFirst;
    public bool CanMoveDown => !_isLast;

    public ICommand MoveUpCommand { get; set; } = new Command(() => { });
    public ICommand MoveDownCommand { get; set; } = new Command(() => { });

    internal void SetPosition(int index, int count)
    {
        _position = index + 1;
        _isFirst = index == 0;
        _isLast = index == count - 1;
        OnPropertyChanged(nameof(PriorityText));
        OnPropertyChanged(nameof(CanMoveUp));
        OnPropertyChanged(nameof(CanMoveDown));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// One tile of the preview grid. Rebuilt whenever the order changes, so it needs no change notification.
public class SkinPreviewEntry
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public ImageSource? Image { get; init; }
    public bool HasImage => Image != null;
    public Aspect Aspect { get; init; } = Aspect.Fill;
    public double TileWidth { get; init; } = 150;
    public double TileHeight { get; init; } = 48;
    public double SlotWidth => Math.Max(TileWidth, 110);
    public string SourceText { get; init; } = string.Empty;
    public Color TitleColor => HasImage ? Colors.White : Colors.Gray;
}

public class SkinSettingsViewModel : INotifyPropertyChanged
{
    private readonly ISkinService _skins;
    private readonly Action _onPendingChanged;
    private List<SkinPackSetting> _savedSnapshot = new();

    // Font picker: FontLabels and _fontKeys run in parallel; index 0 is the default font (key "")
    private readonly List<string> _fontKeys = new();
    private string _selectedFontKey = string.Empty;
    private string _savedFontKey = string.Empty;

    // key, title, tile width, tile height, text shown when no pack provides it
    private static readonly (string Key, string Title, double Width, double Height, string MissingText)[] PreviewSpecs =
    {
        (SkinKeys.Primary,         "Primary",          150, 48, "default look"),
        (SkinKeys.Neutral,         "Neutral",          150, 48, "default look"),
        (SkinKeys.Danger,          "Danger",           150, 48, "default look"),
        (SkinKeys.Warning,         "Warning",          150, 48, "default look"),
        (SkinKeys.Create,          "Create",           150, 48, "default look"),
        (SkinKeys.Confirm,         "Confirm",          150, 48, "default look"),
        (SkinKeys.ConfirmPressed,  "Confirm pressed",  150, 48, "optional - auto shrink"),
        (SkinKeys.ConfirmDisabled, "Confirm disabled", 150, 48, "optional - auto dim"),
        (SkinKeys.Summary,         "Summary",          150, 48, "default look"),
        (SkinKeys.Generic,         "Generic",          150, 48, "default look"),
        (SkinKeys.GenericSmall,    "Small",             48, 48, "default look"),
        (SkinKeys.Panel,           "Panel",            150, 80, "card look"),
        (SkinKeys.ItemRarity0,     "Base",              64, 64, "plain theme border"),
        (SkinKeys.ItemRarity1,     "Common",            64, 64, "green border"),
        (SkinKeys.ItemRarity2,     "Uncommon",          64, 64, "purple border"),
        (SkinKeys.ItemRarity3,     "Rare",              64, 64, "gold border"),
        (SkinKeys.PageBackground,  "Page background",  300, 120, "page color"),
    };

    public SkinSettingsViewModel(ISkinService skins, Action onPendingChanged)
    {
        _skins = skins;
        _onPendingChanged = onPendingChanged;

        _savedFontKey = _skins.SavedFontKey;
        _selectedFontKey = _savedFontKey;

        OpenFolderCommand = new Command(_skins.OpenSkinsFolder);
        RefreshCommand = new Command(RefreshPacks);

        ChooseBodyTextColorCommand = new Command(async () =>
        {
            var popup = new Views.ColorPickerPopup("Body Text", BodyTextColor.Current, color =>
            {
                BodyTextColor.Preview(color);
                OnBodyTextColorEdited();
            });

            await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
        });

        ResetBodyTextColorCommand = new Command(() =>
        {
            BodyTextColor.Preview(null);
            OnBodyTextColorEdited();
        });

        // No notification here: the owning SettingsViewModel isn't fully built yet
        Rebuild(_skins.SavedOrder, notify: false);
    }

    public ObservableCollection<SkinPackEntry> Packs { get; } = new();
    public ObservableCollection<SkinPreviewEntry> Previews { get; } = new();
    public ObservableCollection<string> FontLabels { get; } = new();

    public string SkinsFolderPath => _skins.SkinsFolderPath;
    public bool HasNoPacks => Packs.Count == 0;
    public bool HasPendingChanges { get; private set; }

    public ICommand OpenFolderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ChooseBodyTextColorCommand { get; }
    public ICommand ResetBodyTextColorCommand { get; }

    // ===== Font =====

    public int SelectedFontIndex
    {
        get => Math.Max(0, _fontKeys.FindIndex(k => k.Equals(_selectedFontKey, StringComparison.OrdinalIgnoreCase)));
        set
        {
            // The picker reports -1 while its list is being rebuilt; ignore that
            if (value < 0 || value >= _fontKeys.Count) return;
            if (_fontKeys[value].Equals(_selectedFontKey, StringComparison.OrdinalIgnoreCase)) return;

            _selectedFontKey = _fontKeys[value];
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowRestartNote));
            RecomputePending();
            _onPendingChanged();
        }
    }

    // A font only takes effect on the next launch; this stays visible until then
    public bool ShowRestartNote =>
        !_selectedFontKey.Equals(_skins.AppliedFontKey, StringComparison.OrdinalIgnoreCase);

    // ===== Body text color =====

    public Color BodyTextSwatch => BodyTextColor.Current;

    public string BodyTextColorText =>
        BodyTextColor.HasOverride ? BodyTextColor.Current.ToArgbHex() : "Default (follows Dark / Light)";

    // Called by SettingsViewModel
    public void Save()
    {
        _skins.SaveOrder(CurrentSettings());
        _savedSnapshot = _skins.SavedOrder.ToList();

        bool fontChanged = !_selectedFontKey.Equals(_savedFontKey, StringComparison.OrdinalIgnoreCase);
        _skins.SaveFontKey(_selectedFontKey);
        _savedFontKey = _selectedFontKey;

        BodyTextColor.Save();

        RecomputePending();
        RefreshTextProperties();

        if (fontChanged && ShowRestartNote)
        {
            _ = ShowRestartNoticeAsync();
        }
    }

    public void Discard()
    {
        _selectedFontKey = _savedFontKey;
        BodyTextColor.Discard();
        Rebuild(_savedSnapshot, notify: false);
        RefreshTextProperties();
    }

    // ===== Internals =====

    private async Task ShowRestartNoticeAsync()
    {
        try
        {
            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Restart needed",
                "The new font will be used the next time you open the app. Close and reopen it to see it.",
                "OK");
        }
        catch
        {
            // The on-screen restart note is still showing, so a failed alert is harmless
        }
    }

    private void OnBodyTextColorEdited()
    {
        RefreshTextProperties();
        RecomputePending();
        _onPendingChanged();
    }

    private void RefreshTextProperties()
    {
        OnPropertyChanged(nameof(BodyTextSwatch));
        OnPropertyChanged(nameof(BodyTextColorText));
        OnPropertyChanged(nameof(SelectedFontIndex));
        OnPropertyChanged(nameof(ShowRestartNote));
    }

    private void RefreshPacks()
    {
        // Rescan the folder but keep any unsaved reordering / enabling
        var pending = CurrentSettings();
        _skins.Refresh();
        Rebuild(pending, notify: true);
    }

    private void Rebuild(IEnumerable<SkinPackSetting> shown, bool notify)
    {
        _savedSnapshot = _skins.SavedOrder.ToList();
        _savedFontKey = _skins.SavedFontKey;

        Packs.Clear();
        foreach (var setting in _skins.NormalizeOrder(shown))
        {
            var pack = _skins.InstalledPacks.FirstOrDefault(p =>
                p.FolderName.Equals(setting.Folder, StringComparison.OrdinalIgnoreCase));
            if (pack == null) continue;

            var entry = new SkinPackEntry(pack, setting.Enabled, OnPacksEdited);
            entry.MoveUpCommand = new Command(() => Move(entry, -1));
            entry.MoveDownCommand = new Command(() => Move(entry, +1));
            Packs.Add(entry);
        }

        UpdatePositions();
        RebuildPreviews();
        RebuildFontOptions();
        RecomputePending();
        OnPropertyChanged(nameof(HasNoPacks));

        if (notify) _onPendingChanged();
    }

    private void Move(SkinPackEntry entry, int delta)
    {
        int from = Packs.IndexOf(entry);
        int to = from + delta;
        if (from < 0 || to < 0 || to >= Packs.Count) return;

        Packs.Move(from, to);
        OnPacksEdited();
    }

    private void OnPacksEdited()
    {
        UpdatePositions();
        RebuildPreviews();
        RebuildFontOptions();
        RecomputePending();
        _onPendingChanged();
    }

    private void UpdatePositions()
    {
        for (int i = 0; i < Packs.Count; i++)
        {
            Packs[i].SetPosition(i, Packs.Count);
        }
    }

    private List<SkinPackSetting> CurrentSettings() =>
        Packs.Select(p => new SkinPackSetting { Folder = p.Folder, Enabled = p.IsEnabled }).ToList();

    // Fonts of the enabled packs, in load order. A chosen font whose pack is now off falls back to the default.
    private void RebuildFontOptions()
    {
        FontLabels.Clear();
        _fontKeys.Clear();

        FontLabels.Add("Default font");
        _fontKeys.Add(string.Empty);

        foreach (var font in _skins.GetAvailableFonts(CurrentSettings()))
        {
            FontLabels.Add($"{font.DisplayName} ({font.PackName})");
            _fontKeys.Add(font.Key);
        }

        if (!_fontKeys.Any(k => k.Equals(_selectedFontKey, StringComparison.OrdinalIgnoreCase)))
        {
            _selectedFontKey = string.Empty;
        }

        OnPropertyChanged(nameof(SelectedFontIndex));
        OnPropertyChanged(nameof(ShowRestartNote));
    }

    private void RecomputePending()
    {
        var current = CurrentSettings();
        bool differs = current.Count != _savedSnapshot.Count;

        for (int i = 0; !differs && i < current.Count; i++)
        {
            differs = !current[i].Folder.Equals(_savedSnapshot[i].Folder, StringComparison.OrdinalIgnoreCase)
                      || current[i].Enabled != _savedSnapshot[i].Enabled;
        }

        differs = differs
                  || !_selectedFontKey.Equals(_savedFontKey, StringComparison.OrdinalIgnoreCase)
                  || BodyTextColor.HasUnsavedChange;

        HasPendingChanges = differs;
        OnPropertyChanged(nameof(HasPendingChanges));
    }

    private void RebuildPreviews()
    {
        var order = CurrentSettings();
        Previews.Clear();

        foreach (var spec in PreviewSpecs)
        {
            var resolved = _skins.Resolve(spec.Key, order);

            Previews.Add(new SkinPreviewEntry
            {
                Key = spec.Key,
                Title = spec.Title,
                Image = resolved == null ? null : _skins.LoadImageSource(resolved.FilePath),
                Aspect = spec.Key == SkinKeys.PageBackground ? Aspect.AspectFill : Aspect.Fill,
                TileWidth = spec.Width,
                TileHeight = spec.Height,
                SourceText = resolved != null
                    ? $"{spec.Key} • {resolved.PackName}"
                    : $"{spec.Key} • {spec.MissingText}"
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}