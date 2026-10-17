using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomTraitStorageService : ICustomTraitStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customTraitsFile = "custom_traits.json";

    public CustomTraitStorageService()
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
    }

    public async Task<string> GetStoragePathAsync()
    {
        return await Task.FromResult(_storageDirectory);
    }

    private string GetFullFilePath()
    {
        return Path.Combine(_storageDirectory, _customTraitsFile);
    }

    public async Task<List<CustomTrait>> LoadCustomTraitsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<CustomTrait>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var traits = JsonSerializer.Deserialize<List<CustomTrait>>(json, _jsonOptions);
            return traits ?? new List<CustomTrait>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom traits: {ex.Message}");
            return new List<CustomTrait>();
        }
    }

    public async Task SaveCustomTraitAsync(CustomTrait trait)
    {
        var traits = await LoadCustomTraitsAsync();

        var existing = traits.FirstOrDefault(t => t.Guid == trait.Guid);
        if (existing != null)
        {
            var index = traits.IndexOf(existing);
            traits[index] = trait;
        }
        else
        {
            traits.Add(trait);
        }

        await SaveCustomTraitsAsync(traits);
    }

    public async Task SaveCustomTraitsAsync(List<CustomTrait> traits)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(traits, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom traits: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomTraitAsync(Guid guid)
    {
        var traits = await LoadCustomTraitsAsync();
        var trait = traits.FirstOrDefault(t => t.Guid == guid);
        if (trait == null) return false;

        traits.Remove(trait);
        await SaveCustomTraitsAsync(traits);
        return true;
    }
}