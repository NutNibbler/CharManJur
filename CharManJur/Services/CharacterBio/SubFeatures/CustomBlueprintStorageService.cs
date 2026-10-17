using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomBlueprintStorageService : ICustomBlueprintStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customBlueprintsFile = "custom_blueprints.json";

    public CustomBlueprintStorageService()
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
        return Path.Combine(_storageDirectory, _customBlueprintsFile);
    }

    public async Task<List<Blueprint>> LoadCustomBlueprintsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<Blueprint>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var blueprints = JsonSerializer.Deserialize<List<Blueprint>>(json, _jsonOptions);
            return blueprints ?? new List<Blueprint>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom blueprints: {ex.Message}");
            return new List<Blueprint>();
        }
    }

    public async Task SaveCustomBlueprintAsync(Blueprint blueprint)
    {
        var blueprints = await LoadCustomBlueprintsAsync();

        var existing = blueprints.FirstOrDefault(b => b.Guid == blueprint.Guid);
        if (existing != null)
        {
            var index = blueprints.IndexOf(existing);
            blueprints[index] = blueprint;
        }
        else
        {
            blueprints.Add(blueprint);
        }

        await SaveCustomBlueprintsAsync(blueprints);
    }

    public async Task SaveCustomBlueprintsAsync(List<Blueprint> blueprints)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(blueprints, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom blueprints: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomBlueprintAsync(Guid guid)
    {
        var blueprints = await LoadCustomBlueprintsAsync();
        var blueprint = blueprints.FirstOrDefault(b => b.Guid == guid);
        if (blueprint == null) return false;

        blueprints.Remove(blueprint);
        await SaveCustomBlueprintsAsync(blueprints);
        return true;
    }
}