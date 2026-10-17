using CharManJur.Models;
using System.Diagnostics;
using System.Text.Json;

namespace CharManJur.Services;

public interface ISkinService
{
    string SkinsFolderPath { get; }

    // Every pack folder found on disk
    IReadOnlyList<SkinPack> InstalledPacks { get; }

    // The saved load order (top wins), including disabled packs
    IReadOnlyList<SkinPackSetting> SavedOrder { get; }

    // Fires after the saved order changes or the folder is rescanned
    event EventHandler? SkinsChanged;

    void Initialize();
    void Refresh();
    void SaveOrder(IEnumerable<SkinPackSetting> order);

    // Drops entries for missing packs and appends packs that aren't listed yet (enabled, at the bottom)
    IReadOnlyList<SkinPackSetting> NormalizeOrder(IEnumerable<SkinPackSetting> candidate);

    // Looks a key up through the saved order / through a candidate order (used by the previews)
    ResolvedSkinImage? Resolve(string key);
    ResolvedSkinImage? Resolve(string key, IEnumerable<SkinPackSetting> order);

    ImageSource? LoadImageSource(string? filePath);
    void OpenSkinsFolder();

    // ===== Fonts =====

    // Fonts from the enabled packs of an order, first pack first
    IReadOnlyList<SkinFontInfo> GetAvailableFonts(IEnumerable<SkinPackSetting> order);

    // The saved choice ("PackFolder|FileName"), empty = default font
    string SavedFontKey { get; }

    // The choice that was in effect when this launch started (a new choice needs a restart)
    string AppliedFontKey { get; }
    SkinFontInfo? AppliedFont { get; }

    void SaveFontKey(string key);
}

public class SkinService : ISkinService
{
    private const string OrderPreferenceKey = "Skins_LoadOrder_v1";
    private const string FontPreferenceKey = "Skins_Font_v1";

    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    private static readonly string[] FontExtensions = { ".ttf", ".otf" };

    // skin.json may contain // comments and trailing commas
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly List<SkinPack> _packs = new();
    private List<SkinPackSetting> _order = new();
    private readonly Dictionary<string, (DateTime Stamp, byte[] Bytes)> _imageCache =
        new(StringComparer.OrdinalIgnoreCase);

    private string _savedFontKey = string.Empty;
    private string _appliedFontKey = string.Empty;

    public event EventHandler? SkinsChanged;

    public string SkinsFolderPath => Path.Combine(AppPaths.DataDirectory, "Skins");

    public IReadOnlyList<SkinPack> InstalledPacks => _packs;

    public IReadOnlyList<SkinPackSetting> SavedOrder =>
        _order.Select(s => new SkinPackSetting { Folder = s.Folder, Enabled = s.Enabled }).ToList();

    public string SavedFontKey => _savedFontKey;
    public string AppliedFontKey => _appliedFontKey;
    public SkinFontInfo? AppliedFont => FindFont(_appliedFontKey);

    // ===== Startup / refresh =====

    public void Initialize()
    {
        ScanDisk();
        _order = NormalizeOrder(LoadSavedOrder()).ToList();
        PersistOrder();

        _savedFontKey = Preferences.Default.Get(FontPreferenceKey, string.Empty);
        _appliedFontKey = FindFont(_savedFontKey) != null ? _savedFontKey : string.Empty;
    }

    public void Refresh()
    {
        ScanDisk();
        _imageCache.Clear();
        _order = NormalizeOrder(_order).ToList();
        PersistOrder();
        SkinsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveOrder(IEnumerable<SkinPackSetting> order)
    {
        _order = NormalizeOrder(order).ToList();
        PersistOrder();
        SkinsChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<SkinPackSetting> NormalizeOrder(IEnumerable<SkinPackSetting> candidate)
    {
        var result = new List<SkinPackSetting>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in candidate)
        {
            var pack = _packs.FirstOrDefault(p => p.FolderName.Equals(setting.Folder, StringComparison.OrdinalIgnoreCase));
            if (pack == null || !seen.Add(pack.FolderName)) continue;
            result.Add(new SkinPackSetting { Folder = pack.FolderName, Enabled = setting.Enabled });
        }

        // Newly found packs go to the bottom, enabled
        foreach (var pack in _packs)
        {
            if (seen.Add(pack.FolderName))
            {
                result.Add(new SkinPackSetting { Folder = pack.FolderName, Enabled = true });
            }
        }

        return result;
    }

    // ===== Lookup =====

    public ResolvedSkinImage? Resolve(string key) => Resolve(key, _order);

    public ResolvedSkinImage? Resolve(string key, IEnumerable<SkinPackSetting> order)
    {
        foreach (var setting in order)
        {
            if (!setting.Enabled) continue;

            var pack = _packs.FirstOrDefault(p => p.FolderName.Equals(setting.Folder, StringComparison.OrdinalIgnoreCase));
            if (pack == null || !pack.Images.TryGetValue(key, out var path)) continue;

            return new ResolvedSkinImage
            {
                Key = key,
                FilePath = path,
                PackFolder = pack.FolderName,
                PackName = pack.DisplayName,
                Margins = pack.ImageMargins.TryGetValue(key, out var margins) ? margins : pack.DefaultMargins
            };
        }

        return null;
    }

    // Reads the file into memory so the file isn't locked while the app runs, and edits show up after Refresh
    public ImageSource? LoadImageSource(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

        try
        {
            var stamp = File.GetLastWriteTimeUtc(filePath);
            if (!_imageCache.TryGetValue(filePath, out var cached) || cached.Stamp != stamp)
            {
                cached = (stamp, File.ReadAllBytes(filePath));
                _imageCache[filePath] = cached;
            }

            var bytes = cached.Bytes;
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinService: could not load '{filePath}': {ex.Message} ===");
            return null;
        }
    }

    public void OpenSkinsFolder()
    {
        try
        {
            Directory.CreateDirectory(SkinsFolderPath);
            Process.Start(new ProcessStartInfo { FileName = SkinsFolderPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinService: could not open skins folder: {ex.Message} ===");
        }
    }

    // ===== Fonts =====

    public IReadOnlyList<SkinFontInfo> GetAvailableFonts(IEnumerable<SkinPackSetting> order)
    {
        var result = new List<SkinFontInfo>();

        foreach (var setting in order)
        {
            if (!setting.Enabled) continue;

            var pack = _packs.FirstOrDefault(p => p.FolderName.Equals(setting.Folder, StringComparison.OrdinalIgnoreCase));
            if (pack == null) continue;

            result.AddRange(pack.Fonts);
        }

        return result;
    }

    public void SaveFontKey(string key)
    {
        _savedFontKey = key ?? string.Empty;
        Preferences.Default.Set(FontPreferenceKey, _savedFontKey);
    }

    private SkinFontInfo? FindFont(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return GetAvailableFonts(_order).FirstOrDefault(f => f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    // ===== Disk scan =====

    private void ScanDisk()
    {
        _packs.Clear();

        try
        {
            Directory.CreateDirectory(SkinsFolderPath);

            var folders = Directory.EnumerateDirectories(SkinsFolderPath)
                .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                try
                {
                    _packs.Add(LoadPack(folder));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"=== SkinService: could not read pack '{folder}': {ex.Message} ===");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SkinService: could not scan skins folder: {ex.Message} ===");
        }
    }

    private static SkinPack LoadPack(string folder)
    {
        var pack = new SkinPack
        {
            FolderName = Path.GetFileName(folder),
            FolderPath = folder
        };
        pack.DisplayName = pack.FolderName;

        // Subfolders don't matter: match by file name. Shallower files win a tie inside one pack.
        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .OrderBy(f => f.Count(c => c == Path.DirectorySeparatorChar))
            .ThenBy(f => f, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();

            if (ImageExtensions.Contains(ext))
            {
                pack.Images.TryAdd(Path.GetFileNameWithoutExtension(file), file);
            }
            else if (FontExtensions.Contains(ext))
            {
                pack.Fonts.Add(new SkinFontInfo
                {
                    PackFolder = pack.FolderName,
                    FileName = Path.GetFileName(file),
                    FullPath = file,
                    DisplayName = Path.GetFileNameWithoutExtension(file)
                });
            }
        }

        string jsonPath = Path.Combine(folder, "skin.json");
        if (File.Exists(jsonPath))
        {
            ReadSkinJson(pack, jsonPath);
        }

        foreach (var font in pack.Fonts)
        {
            font.PackName = pack.DisplayName;
        }

        return pack;
    }

    private static void ReadSkinJson(SkinPack pack, string jsonPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath), JsonOptions);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                pack.Warning = "skin.json is not a JSON object, so it was ignored.";
                return;
            }

            string? name = ReadString(root, "name");
            if (!string.IsNullOrWhiteSpace(name)) pack.DisplayName = name.Trim();
            pack.Author = ReadString(root, "author") ?? string.Empty;
            pack.Version = ReadString(root, "version") ?? string.Empty;
            pack.Description = ReadString(root, "description") ?? string.Empty;

            // Nine-slice: read "default" first so per-image entries with missing sides can fall back to it
            if (root.TryGetProperty("nineSlice", out var nineSlice) && nineSlice.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in nineSlice.EnumerateObject())
                {
                    if (prop.Name.Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        var parsed = ParseMargins(prop.Value, SkinPack.DefaultNineSlice);
                        if (parsed != null) pack.DefaultMargins = parsed.Value;
                    }
                }

                foreach (var prop in nineSlice.EnumerateObject())
                {
                    if (prop.Name.Equals("default", StringComparison.OrdinalIgnoreCase)) continue;

                    var parsed = ParseMargins(prop.Value, pack.DefaultMargins.Left);
                    if (parsed != null) pack.ImageMargins[prop.Name] = parsed.Value;
                }
            }

            // Fonts: friendly names for files that actually exist in the pack
            if (root.TryGetProperty("fonts", out var fonts) && fonts.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in fonts.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object) continue;

                    string? file = ReadString(entry, "file");
                    string? friendly = ReadString(entry, "name");
                    if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(friendly)) continue;

                    var match = pack.Fonts.FirstOrDefault(f =>
                        f.FileName.Equals(file.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (match != null) match.DisplayName = friendly.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            pack.Warning = $"skin.json could not be read ({ex.Message}). Defaults were used.";
        }
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    // A number means all four corners; an object can give left/top/right/bottom (missing sides use the fallback)
    private static NineSliceMargins? ParseMargins(JsonElement element, double fallback)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double uniform))
        {
            return NineSliceMargins.Uniform(Math.Max(0, uniform));
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            return new NineSliceMargins(
                ReadSide(element, "left", fallback),
                ReadSide(element, "top", fallback),
                ReadSide(element, "right", fallback),
                ReadSide(element, "bottom", fallback));
        }

        return null;
    }

    private static double ReadSide(JsonElement element, string property, double fallback) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double d)
            ? Math.Max(0, d)
            : fallback;

    // ===== Saved load order =====

    private static List<SkinPackSetting> LoadSavedOrder()
    {
        string json = Preferences.Default.Get(OrderPreferenceKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return new();

        try
        {
            return JsonSerializer.Deserialize<List<SkinPackSetting>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void PersistOrder()
    {
        Preferences.Default.Set(OrderPreferenceKey, JsonSerializer.Serialize(_order));
    }
}