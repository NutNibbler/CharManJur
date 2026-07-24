using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomBackgroundStorageService : ICustomBackgroundStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customBackgroundsFile = "Custom_Background.json";
    private readonly int _customIdStart = 90001;

    public CustomBackgroundStorageService()
    {
        string appDataPath = FileSystem.AppDataDirectory;
        _storageDirectory = Path.Combine(appDataPath, "CustomItems");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<string> GetStoragePathAsync()
    {
        return await Task.FromResult(_storageDirectory);
    }

    private string GetFullFilePath()
    {
        return Path.Combine(_storageDirectory, _customBackgroundsFile);
    }

    public async Task<List<CharacterBackground>> LoadCustomBackgroundsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<CharacterBackground>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var backgrounds = JsonSerializer.Deserialize<List<CharacterBackground>>(json, _jsonOptions);
            return backgrounds ?? new List<CharacterBackground>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom backgrounds: {ex.Message}");
            return new List<CharacterBackground>();
        }
    }

    public async Task SaveCustomBackgroundAsync(CharacterBackground background)
    {
        var backgrounds = await LoadCustomBackgroundsAsync();

        var existing = backgrounds.FirstOrDefault(b => b.Id == background.Id);
        if (existing != null)
        {
            var index = backgrounds.IndexOf(existing);
            backgrounds[index] = background;
        }
        else
        {
            backgrounds.Add(background);
        }

        await SaveCustomBackgroundsAsync(backgrounds);
    }

    public async Task SaveCustomBackgroundsAsync(List<CharacterBackground> backgrounds)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(backgrounds, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom backgrounds: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomBackgroundAsync(int id)
    {
        var backgrounds = await LoadCustomBackgroundsAsync();
        var background = backgrounds.FirstOrDefault(b => b.Id == id);
        if (background == null) return false;

        backgrounds.Remove(background);
        await SaveCustomBackgroundsAsync(backgrounds);
        return true;
    }

    public async Task<int> GetNextCustomBackgroundIdAsync()
    {
        var backgrounds = await LoadCustomBackgroundsAsync();
        if (!backgrounds.Any())
        {
            return _customIdStart;
        }

        var customIds = backgrounds.Select(b => b.Id).Where(id => id >= _customIdStart).ToList();
        if (!customIds.Any())
        {
            return _customIdStart;
        }

        customIds.Sort();
        int nextId = _customIdStart;
        foreach (var id in customIds)
        {
            if (id == nextId)
            {
                nextId++;
            }
            else if (id > nextId)
            {
                break;
            }
        }
        return nextId;
    }

    public async Task<bool> CustomBackgroundExistsAsync(int id)
    {
        var backgrounds = await LoadCustomBackgroundsAsync();
        return backgrounds.Any(b => b.Id == id);
    }
}