using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomTechniqueStorageService : ICustomTechniqueStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customTechniquesFile = "custom_techniques.json";

    public CustomTechniqueStorageService()
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
        return Path.Combine(_storageDirectory, _customTechniquesFile);
    }

    public async Task<List<Technique>> LoadCustomTechniquesAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<Technique>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var techniques = JsonSerializer.Deserialize<List<Technique>>(json, _jsonOptions);
            return techniques ?? new List<Technique>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom techniques: {ex.Message}");
            return new List<Technique>();
        }
    }

    public async Task SaveCustomTechniqueAsync(Technique technique)
    {
        var techniques = await LoadCustomTechniquesAsync();

        var existing = techniques.FirstOrDefault(t => t.Guid == technique.Guid);
        if (existing != null)
        {
            var index = techniques.IndexOf(existing);
            techniques[index] = technique;
        }
        else
        {
            techniques.Add(technique);
        }

        await SaveCustomTechniquesAsync(techniques);
    }

    public async Task SaveCustomTechniquesAsync(List<Technique> techniques)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(techniques, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom techniques: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomTechniqueAsync(Guid guid)
    {
        var techniques = await LoadCustomTechniquesAsync();
        var technique = techniques.FirstOrDefault(t => t.Guid == guid);
        if (technique == null) return false;

        techniques.Remove(technique);
        await SaveCustomTechniquesAsync(techniques);
        return true;
    }
}