using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomRaceStorageService : ICustomRaceStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customRacesFile = "Custom_Race.json";
    private readonly int _customIdStart = 90001;

    public CustomRaceStorageService()
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
        return Path.Combine(_storageDirectory, _customRacesFile);
    }

    public async Task<List<Race>> LoadCustomRacesAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<Race>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var races = JsonSerializer.Deserialize<List<Race>>(json, _jsonOptions);
            return races ?? new List<Race>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom races: {ex.Message}");
            return new List<Race>();
        }
    }

    public async Task SaveCustomRaceAsync(Race race)
    {
        var races = await LoadCustomRacesAsync();

        var existing = races.FirstOrDefault(r => r.Id == race.Id);
        if (existing != null)
        {
            var index = races.IndexOf(existing);
            races[index] = race;
        }
        else
        {
            races.Add(race);
        }

        await SaveCustomRacesAsync(races);
    }

    public async Task SaveCustomRacesAsync(List<Race> races)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(races, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom races: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomRaceAsync(int id)
    {
        var races = await LoadCustomRacesAsync();
        var race = races.FirstOrDefault(r => r.Id == id);
        if (race == null) return false;

        races.Remove(race);
        await SaveCustomRacesAsync(races);
        return true;
    }

    public async Task<int> GetNextCustomRaceIdAsync()
    {
        var races = await LoadCustomRacesAsync();
        if (!races.Any())
        {
            return _customIdStart;
        }

        // Get all custom IDs (should be >= 90001)
        var customIds = races.Select(r => r.Id).Where(id => id >= _customIdStart).ToList();
        if (!customIds.Any())
        {
            return _customIdStart;
        }

        // Find the next available ID starting from 90001
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

    public async Task<bool> CustomRaceExistsAsync(int id)
    {
        var races = await LoadCustomRacesAsync();
        return races.Any(r => r.Id == id);
    }
}