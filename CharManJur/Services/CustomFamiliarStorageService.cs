using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomFamiliarStorageService : ICustomFamiliarStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customFamiliarsFile = "custom_familiars.json";
    private readonly int _customIdStart = 80001;

    public CustomFamiliarStorageService()
    {
        string appDataPath = FileSystem.AppDataDirectory;
        _storageDirectory = Path.Combine(appDataPath, "CustomItems");

        System.Diagnostics.Debug.WriteLine($"Full JSON Path:    {GetFullFilePath(),-43}");

        if (!Directory.Exists(_storageDirectory))
        {
            System.Diagnostics.Debug.WriteLine("JSON DIRECTORY DOESN'T EXIST: CREATING NEW ONE");
            Directory.CreateDirectory(_storageDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        System.Diagnostics.Debug.WriteLine($"=== CUSTOM FAMILIAR STORAGE ===");
        System.Diagnostics.Debug.WriteLine($"Storage Path: {_storageDirectory}");
    }

    public async Task<string> GetStoragePathAsync()
    {
        return await Task.FromResult(_storageDirectory);
    }

    private string GetFullFilePath()
    {
        return Path.Combine(_storageDirectory, _customFamiliarsFile);
    }

    public async Task<List<Familiar>> LoadCustomFamiliarsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine("No custom familiars file found, returning empty list.");
            return new List<Familiar>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var familiars = JsonSerializer.Deserialize<List<Familiar>>(json, _jsonOptions);
            return familiars ?? new List<Familiar>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom familiars: {ex.Message}");
            return new List<Familiar>();
        }
    }

    public async Task SaveCustomFamiliarAsync(Familiar familiar)
    {
        var familiars = await LoadCustomFamiliarsAsync();

        var existing = familiars.FirstOrDefault(f => f.Id == familiar.Id);
        if (existing != null)
        {
            var index = familiars.IndexOf(existing);
            familiars[index] = familiar;
        }
        else
        {
            familiars.Add(familiar);
        }

        await SaveCustomFamiliarsAsync(familiars);
    }

    public async Task SaveCustomFamiliarsAsync(List<Familiar> familiars)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(familiars, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            System.Diagnostics.Debug.WriteLine($"Saved {familiars.Count} custom familiars to {filePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom familiars: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomFamiliarAsync(int id)
    {
        var familiars = await LoadCustomFamiliarsAsync();
        var familiar = familiars.FirstOrDefault(f => f.Id == id);
        if (familiar == null) return false;

        familiars.Remove(familiar);
        await SaveCustomFamiliarsAsync(familiars);
        return true;
    }

    public async Task<int> GetNextCustomFamiliarIdAsync()
    {
        var familiars = await LoadCustomFamiliarsAsync();
        if (!familiars.Any())
        {
            return _customIdStart;
        }

        var customIds = familiars.Select(f => f.Id).Where(id => id >= _customIdStart).ToList();
        if (!customIds.Any())
        {
            return _customIdStart;
        }

        customIds.Sort();
        int nextId = _customIdStart;
        foreach (var id in customIds)
        {
            if (id == nextId) nextId++;
            else if (id > nextId) break;
        }
        return nextId;
    }

    public async Task<bool> CustomFamiliarExistsAsync(int id)
    {
        var familiars = await LoadCustomFamiliarsAsync();
        return familiars.Any(f => f.Id == id);
    }
}