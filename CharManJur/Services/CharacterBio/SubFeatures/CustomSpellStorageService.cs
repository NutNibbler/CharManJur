using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomSpellStorageService : ICustomSpellStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customSpellsFile = "custom_spells.json";

    public CustomSpellStorageService()
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
        return Path.Combine(_storageDirectory, _customSpellsFile);
    }

    public async Task<List<Spell>> LoadCustomSpellsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<Spell>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var spells = JsonSerializer.Deserialize<List<Spell>>(json, _jsonOptions);
            return spells ?? new List<Spell>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom spells: {ex.Message}");
            return new List<Spell>();
        }
    }

    public async Task SaveCustomSpellAsync(Spell spell)
    {
        var spells = await LoadCustomSpellsAsync();

        var existing = spells.FirstOrDefault(s => s.Guid == spell.Guid);
        if (existing != null)
        {
            var index = spells.IndexOf(existing);
            spells[index] = spell;
        }
        else
        {
            spells.Add(spell);
        }

        await SaveCustomSpellsAsync(spells);
    }

    public async Task SaveCustomSpellsAsync(List<Spell> spells)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(spells, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom spells: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomSpellAsync(Guid guid)
    {
        var spells = await LoadCustomSpellsAsync();
        var spell = spells.FirstOrDefault(s => s.Guid == guid);
        if (spell == null) return false;

        spells.Remove(spell);
        await SaveCustomSpellsAsync(spells);
        return true;
    }
}