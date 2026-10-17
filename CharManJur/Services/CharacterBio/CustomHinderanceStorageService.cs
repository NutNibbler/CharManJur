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

    public CustomHinderanceStorageService()
    {
        string appDataPath = AppPaths.DataDirectory;
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

        var existing = hinderances.FirstOrDefault(h => h.Guid == hinderance.Guid);
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

    public async Task<bool> DeleteCustomHinderanceAsync(Guid guid)
    {
        var hinderances = await LoadCustomHinderancesAsync();
        var hinderance = hinderances.FirstOrDefault(h => h.Guid == guid);
        if (hinderance == null) return false;

        hinderances.Remove(hinderance);
        await SaveCustomHinderancesAsync(hinderances);
        return true;
    }
}