using System.Text.Json;
using CharManJur.Models;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CharManJur.Services;

// Reads and writes one character's scratch pad files:
//   <data>/ScratchPad/<character guid>/index.json and <page id>.json
// Writes go to a temp file first, so an interrupted save never leaves a half-written page.
public class ScratchPadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions CompactOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Guid _characterGuid;
    private readonly string _folder;

    public static string RootFolder => Path.Combine(AppPaths.DataDirectory, "ScratchPad");

    public ScratchPadStore(Guid characterGuid)
    {
        _characterGuid = characterGuid;
        _folder = Path.Combine(RootFolder, characterGuid.ToString("D"));
    }

    // A pad that isn't a character's (the GM's): the same files, kept in a folder of their choosing.
    public ScratchPadStore(string folder)
    {
        _characterGuid = Guid.Empty;
        _folder = folder;
    }

    private string IndexPath => Path.Combine(_folder, "index.json");
    private string PagePath(Guid id) => Path.Combine(_folder, $"{id:D}.json");

    public async Task<ScratchIndex> LoadIndexAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var index = await ReadAsync<ScratchIndex>(IndexPath) ?? new ScratchIndex();
            index.CharacterGuid = _characterGuid;
            return index;
        }
        finally { _gate.Release(); }
    }

    public async Task SaveIndexAsync(ScratchIndex index)
    {
        await _gate.WaitAsync();
        try { await WriteAsync(IndexPath, index); }
        finally { _gate.Release(); }
    }

    public async Task<ScratchPageData?> LoadPageAsync(Guid id)
    {
        await _gate.WaitAsync();
        try { return await ReadAsync<ScratchPageData>(PagePath(id)); }
        finally { _gate.Release(); }
    }

    public async Task SavePageAsync(ScratchPageData page)
    {
        await _gate.WaitAsync();
        try { await WriteAsync(PagePath(page.Id), page, indent: false); }
        finally { _gate.Release(); }
    }

    public async Task DeletePageAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            string path = PagePath(id);
            if (File.Exists(path)) File.Delete(path);

            // The page's background image(s) go with it
            if (Directory.Exists(_folder))
                foreach (var image in Directory.EnumerateFiles(_folder, $"{id:D}.bg.*"))
                    DeleteFileQuietly(image);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SCRATCH] Delete failed: {ex.Message}"); }
        finally { _gate.Release(); }
    }

    // ===== BACKGROUND IMAGES =====
    private const float MaxImageSide = 2048f;

    // Copies a picked image into the character's folder (downscaled, as a JPEG) and returns its file name.
    // Each import gets a new name so the drawing view knows to reload. The previous image is removed.
    public async Task<string?> ImportBackgroundAsync(Guid pageId, Stream source, string? oldName)
    {
        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(_folder);
            string name = $"{pageId:D}.bg.{Guid.NewGuid().ToString("N")[..8]}.jpg";
            string path = Path.Combine(_folder, name);
            string temp = path + ".tmp";

            await Task.Run(() =>
            {
                using var original = PlatformImage.FromStream(source);
                using var fitted = original.Downsize(MaxImageSide);
                using var file = File.Create(temp);
                fitted.Save(file, ImageFormat.Jpeg, 0.85f);
            });

            File.Move(temp, path, overwrite: true);
            if (!string.IsNullOrEmpty(oldName)) DeleteFileQuietly(Path.Combine(_folder, Path.GetFileName(oldName)));
            return name;
        }
        finally { _gate.Release(); }
    }

    public IImage? LoadBackground(string fileName)
    {
        try
        {
            string path = Path.Combine(_folder, Path.GetFileName(fileName));   // file name only, never a path
            if (!File.Exists(path)) return null;
            using var stream = File.OpenRead(path);
            return PlatformImage.FromStream(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SCRATCH] Couldn't load background: {ex.Message}");
            return null;
        }
    }

    public void DeleteBackground(string? fileName)
    {
        if (!string.IsNullOrEmpty(fileName)) DeleteFileQuietly(Path.Combine(_folder, Path.GetFileName(fileName)));
    }

    public Stream? OpenBackground(string fileName)
    {
        try
        {
            string path = Path.Combine(_folder, Path.GetFileName(fileName));   // file name only, never a path
            return File.Exists(path) ? File.OpenRead(path) : null;
        }
        catch { return null; }
    }

    // Copies one page (its text or drawing, and its background picture) from one pad into another as a new page.
    // The picture goes through the normal import, so it is downscaled like any picture in a character's pad.
    public static async Task<ScratchPageInfo?> CopyPageAsync(ScratchPadStore from, ScratchPadStore to, Guid pageId, string newName)
    {
        var index = await from.LoadIndexAsync();
        var info = index.Pages.FirstOrDefault(p => p.Id == pageId);
        var page = await from.LoadPageAsync(pageId);
        if (info == null || page == null) return null;

        var copy = new ScratchPageData
        {
            Id = Guid.NewGuid(),
            Type = page.Type,
            Text = page.Text,
            // A real copy of the shapes, so the two pads never share them
            Shapes = JsonSerializer.Deserialize<List<DrawShape>>(JsonSerializer.Serialize(page.Shapes, CompactOptions), CompactOptions) ?? new()
        };

        if (!string.IsNullOrEmpty(page.BackgroundImage))
        {
            using var stream = from.OpenBackground(page.BackgroundImage);
            if (stream != null) copy.BackgroundImage = await to.ImportBackgroundAsync(copy.Id, stream, null);
        }

        await to.SavePageAsync(copy);

        var target = await to.LoadIndexAsync();
        var added = new ScratchPageInfo { Id = copy.Id, Name = newName, Type = info.Type };
        target.Pages.Add(added);
        await to.SaveIndexAsync(target);
        return added;
    }

    private static void DeleteFileQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private async Task WriteAsync(string path, object value, bool indent = true)
    {
        Directory.CreateDirectory(_folder);
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
            System.Diagnostics.Debug.WriteLine($"[SCRATCH] Couldn't read {Path.GetFileName(path)}: {ex.Message}");
            try { File.Move(path, path + ".bad", overwrite: true); } catch { }
            return null;
        }
    }

    // Removes every scratch pad file of one character. Never throws: a leftover folder is harmless.
    public static void DeleteAllFor(Guid characterGuid)
    {
        if (characterGuid == Guid.Empty) return;

        try
        {
            string folder = Path.Combine(RootFolder, characterGuid.ToString("D"));
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SCRATCH] Couldn't delete scratch pad folder: {ex.Message}");
        }
    }
}