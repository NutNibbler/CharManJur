using System.Text.Json;
using CharManJur.Helpers;
using CharManJur.Models;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.Services;

public sealed class MapImportResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Resized { get; init; }
    public int OriginalWidth { get; init; }
    public int OriginalHeight { get; init; }

    public static MapImportResult Fail(string message) => new() { Success = false, Message = message };
}

// One campaign's maps and symbol library:
//   <maps>/index.json                       the list of maps
//   <maps>/<map id>/map.json                shapes and placed symbols
//   <maps>/<map id>/original.*              the imported picture, never changed
//   <maps>/<map id>/working.*               a 3000 px copy, only for pictures bigger than that
//   <symbols>/library.json and <symbol id>.png
// Writes go to a temp file first, so an interrupted save never leaves a half-written file.
public sealed class MapStore
{
    public const float MaxWorkingSide = 3000f;
    private const float MaxSymbolSide = 512f;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions CompactOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _mapsFolder;
    private readonly string _symbolsFolder;

    public MapStore(string mapsFolder, string symbolsFolder)
    {
        _mapsFolder = mapsFolder;
        _symbolsFolder = symbolsFolder;
    }

    private string IndexPath => Path.Combine(_mapsFolder, "index.json");
    private string MapFolder(Guid id) => Path.Combine(_mapsFolder, id.ToString("N"));
    private string MapPath(Guid id) => Path.Combine(MapFolder(id), "map.json");
    private string LibraryPath => Path.Combine(_symbolsFolder, "library.json");

    // ===== MAP LIST =====
    public async Task<MapCatalog> LoadIndexAsync()
    {
        await _gate.WaitAsync();
        try { return await ReadAsync<MapCatalog>(IndexPath) ?? new MapCatalog(); }
        finally { _gate.Release(); }
    }

    public async Task SaveIndexAsync(MapCatalog index)
    {
        await _gate.WaitAsync();
        try { await WriteAsync(IndexPath, index, indent: true); }
        finally { _gate.Release(); }
    }

    // ===== MAPS =====
    public async Task<MapData?> LoadMapAsync(Guid id)
    {
        await _gate.WaitAsync();
        try { return await ReadAsync<MapData>(MapPath(id)); }
        finally { _gate.Release(); }
    }

    public async Task SaveMapAsync(MapData map)
    {
        await _gate.WaitAsync();
        try { await WriteAsync(MapPath(map.Id), map, indent: false); }
        finally { _gate.Release(); }
    }

    public async Task DeleteMapAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            string folder = MapFolder(id);
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MAP] Delete failed: {ex.Message}"); }
        finally { _gate.Release(); }
    }

    // ===== MAP PICTURE =====
    // Keeps the picked file untouched. A picture bigger than 3000 px on its long side also gets a 3000 px
    // working copy, which is what the board draws. The map's previous picture files are removed.
    public async Task<MapImportResult> ImportImageAsync(MapData map, Stream source, string extension)
    {
        await _gate.WaitAsync();
        string? newOriginal = null;
        string? newWorking = null;
        try
        {
            string folder = MapFolder(map.Id);
            Directory.CreateDirectory(folder);

            string ext = (extension ?? string.Empty).ToLowerInvariant();
            if (ext.Length == 0 || ext.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) ext = ".img";

            string stamp = Guid.NewGuid().ToString("N")[..8];
            string originalName = $"original.{stamp}{ext}";
            string originalPath = Path.Combine(folder, originalName);
            newOriginal = originalPath;

            await using (var file = File.Create(originalPath))
                await source.CopyToAsync(file);

            string displayName = originalName;
            int width = 0, height = 0;
            bool resized = false;
            string? workingCreated = null;

            await Task.Run(() =>
            {
                using var stream = File.OpenRead(originalPath);
                using var image = PlatformImage.FromStream(stream);
                width = (int)image.Width;
                height = (int)image.Height;

                if (MathF.Max(image.Width, image.Height) > MaxWorkingSide)
                {
                    bool png = ext == ".png";
                    string workingName = $"working.{stamp}{(png ? ".png" : ".jpg")}";
                    string workingPath = Path.Combine(folder, workingName);

                    using var fitted = image.Downsize(MaxWorkingSide);
                    using var outFile = File.Create(workingPath);
                    fitted.Save(outFile, png ? ImageFormat.Png : ImageFormat.Jpeg, 0.9f);

                    workingCreated = workingPath;
                    displayName = workingName;
                    resized = true;
                }
            });
            newWorking = workingCreated;

            // The new picture is in place: the old one's files go.
            string? oldOriginal = map.OriginalFile, oldImage = map.ImageFile;
            map.OriginalFile = originalName;
            map.ImageFile = displayName;
            if (oldOriginal != null) DeleteFileQuietly(Path.Combine(folder, Path.GetFileName(oldOriginal)));
            if (oldImage != null && oldImage != oldOriginal) DeleteFileQuietly(Path.Combine(folder, Path.GetFileName(oldImage)));

            return new MapImportResult { Success = true, Resized = resized, OriginalWidth = width, OriginalHeight = height };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Import failed: {ex.Message}");
            if (newOriginal != null) DeleteFileQuietly(newOriginal);
            if (newWorking != null) DeleteFileQuietly(newWorking);
            return MapImportResult.Fail("That image couldn't be read. Try a PNG or JPG.");
        }
        finally { _gate.Release(); }
    }

    public async Task RemoveImageAsync(MapData map)
    {
        await _gate.WaitAsync();
        try
        {
            string folder = MapFolder(map.Id);
            if (map.OriginalFile != null) DeleteFileQuietly(Path.Combine(folder, Path.GetFileName(map.OriginalFile)));
            if (map.ImageFile != null) DeleteFileQuietly(Path.Combine(folder, Path.GetFileName(map.ImageFile)));
            map.OriginalFile = null;
            map.ImageFile = null;
        }
        finally { _gate.Release(); }
    }

    public IImage? LoadImage(Guid mapId, string fileName)
        => LoadImageFile(Path.Combine(MapFolder(mapId), Path.GetFileName(fileName)));

    // ===== SYMBOL LIBRARY =====
    public async Task<SymbolLibrary> LoadLibraryAsync()
    {
        await _gate.WaitAsync();
        try { return await ReadLibraryAsync(); }
        finally { _gate.Release(); }
    }

    // The first time, the library starts with a few plain colored tokens.
    private async Task<SymbolLibrary> ReadLibraryAsync()
    {
        var library = await ReadAsync<SymbolLibrary>(LibraryPath);
        if (library != null) return library;

        library = new SymbolLibrary();
        foreach (string color in MapTokenColors.Names.Take(6))
            library.Symbols.Add(new SymbolDef { Name = $"{color} token", Kind = SymbolKind.Token, Color = color });

        await WriteAsync(LibraryPath, library, indent: true);
        return library;
    }

    public async Task<SymbolDef?> ImportSymbolAsync(Stream source, string name)
    {
        await _gate.WaitAsync();
        string? temp = null;
        try
        {
            Directory.CreateDirectory(_symbolsFolder);

            var def = new SymbolDef { Name = string.IsNullOrWhiteSpace(name) ? "Symbol" : name.Trim(), Kind = SymbolKind.Image };
            def.FileName = $"{def.Id:N}.png";
            string path = Path.Combine(_symbolsFolder, def.FileName);
            temp = path + ".tmp";

            await Task.Run(() =>
            {
                using var original = PlatformImage.FromStream(source);
                IImage fitted = MathF.Max(original.Width, original.Height) > MaxSymbolSide
                    ? original.Downsize(MaxSymbolSide)
                    : original;
                try
                {
                    using var file = File.Create(temp);
                    fitted.Save(file, ImageFormat.Png);
                }
                finally
                {
                    if (!ReferenceEquals(fitted, original)) fitted.Dispose();
                }
            });
            File.Move(temp, path, overwrite: true);

            var library = await ReadLibraryAsync();
            library.Symbols.Add(def);
            await WriteAsync(LibraryPath, library, indent: true);
            return def;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Symbol import failed: {ex.Message}");
            if (temp != null) DeleteFileQuietly(temp);
            return null;
        }
        finally { _gate.Release(); }
    }

    public async Task<SymbolDef> AddTokenAsync(string letter, string color)
    {
        await _gate.WaitAsync();
        try
        {
            letter = (letter ?? string.Empty).Trim();
            if (letter.Length > 3) letter = letter[..3];

            var def = new SymbolDef
            {
                Name = letter.Length > 0 ? $"{color} {letter}" : $"{color} token",
                Kind = SymbolKind.Token,
                Letter = letter,
                Color = color
            };

            var library = await ReadLibraryAsync();
            library.Symbols.Add(def);
            await WriteAsync(LibraryPath, library, indent: true);
            return def;
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteSymbolAsync(SymbolDef def)
    {
        await _gate.WaitAsync();
        try
        {
            var library = await ReadLibraryAsync();
            library.Symbols.RemoveAll(s => s.Id == def.Id);
            await WriteAsync(LibraryPath, library, indent: true);

            if (def.FileName != null) DeleteFileQuietly(Path.Combine(_symbolsFolder, Path.GetFileName(def.FileName)));
        }
        finally { _gate.Release(); }
    }

    public IImage? LoadSymbolImage(SymbolDef def)
        => def.FileName == null ? null : LoadImageFile(Path.Combine(_symbolsFolder, Path.GetFileName(def.FileName)));

    public Stream? OpenSymbol(SymbolDef def)
    {
        try
        {
            if (def.FileName == null) return null;
            string path = Path.Combine(_symbolsFolder, Path.GetFileName(def.FileName));
            return File.Exists(path) ? File.OpenRead(path) : null;
        }
        catch { return null; }
    }

    // ===== FILES =====
    private static IImage? LoadImageFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            using var stream = File.OpenRead(path);
            return PlatformImage.FromStream(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Couldn't load {Path.GetFileName(path)}: {ex.Message}");
            return null;
        }
    }

    private static void DeleteFileQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static async Task WriteAsync(string path, object value, bool indent)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(value, value.GetType(), indent ? JsonOptions : CompactOptions));
        File.Move(temp, path, overwrite: true);
    }

    // A file that can't be read is kept as .bad (never overwritten), and treated as missing.
    private static async Task<T?> ReadAsync<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), JsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Couldn't read {Path.GetFileName(path)}: {ex.Message}");
            try { File.Move(path, path + ".bad", overwrite: true); } catch { }
            return null;
        }
    }
}