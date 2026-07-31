using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomHinderanceStorageService : ICustomHinderanceStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customHinderancesFile = "custom_hinderances.json";
    private readonly int _customIdStart = 70001;

    public CustomHinderanceStorageService()
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

        System.Diagnostics.Debug.WriteLine($"=== CUSTOM HINDERANCE STORAGE ===");
        System.Diagnostics.Debug.WriteLine($"Storage Path: {GetFullFilePath()}");
    }

    public async Task<string> GetStoragePathAsync()
    {
        return await Task.FromResult(_storageDirectory);
    }

    private string GetFullFilePath()
    {
        return Path.Combine(_storageDirectory, _customHinderancesFile);
    }

    public async Task<List<Hinderance>> LoadCustomHinderancesAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<Hinderance>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var hinderances = JsonSerializer.Deserialize<List<Hinderance>>(json, _jsonOptions);
            return hinderances ?? new List<Hinderance>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom hinderances: {ex.Message}");
            return new List<Hinderance>();
        }
    }

    public async Task SaveCustomHinderanceAsync(Hinderance hinderance)
    {
        var hinderances = await LoadCustomHinderancesAsync();

        var existing = hinderances.FirstOrDefault(h => h.Id == hinderance.Id);
        if (existing != null)
        {
            var index = hinderances.IndexOf(existing);
            hinderances[index] = hinderance;
        }
        else
        {
            hinderances.Add(hinderance);
        }

        await SaveCustomHinderancesAsync(hinderances);
    }

    public async Task SaveCustomHinderancesAsync(List<Hinderance> hinderances)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(hinderances, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom hinderances: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomHinderanceAsync(int id)
    {
        var hinderances = await LoadCustomHinderancesAsync();
        var hinderance = hinderances.FirstOrDefault(h => h.Id == id);
        if (hinderance == null) return false;

        hinderances.Remove(hinderance);
        await SaveCustomHinderancesAsync(hinderances);
        return true;
    }

    public async Task<int> GetNextCustomHinderanceIdAsync()
    {
        var hinderances = await LoadCustomHinderancesAsync();
        if (!hinderances.Any())
        {
            return _customIdStart;
        }

        var customIds = hinderances.Select(h => h.Id).Where(id => id >= _customIdStart).ToList();
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

    public async Task<bool> CustomHinderanceExistsAsync(int id)
    {
        var hinderances = await LoadCustomHinderancesAsync();
        return hinderances.Any(h => h.Id == id);
    }
}