using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomItemStorageService : ICustomItemStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customItemsFile = "custom_items.json";

    public CustomItemStorageService()
    {
        string appDataPath = FileSystem.AppDataDirectory;
        _storageDirectory = Path.Combine(appDataPath, "CustomItems");

        System.Diagnostics.Debug.WriteLine($"Full JSON Path:    {GetFullFilePath(),-43}");

        // Create directory if it doesn't exist
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

        System.Diagnostics.Debug.WriteLine($"=== CUSTOM ITEM STORAGE ===");
        System.Diagnostics.Debug.WriteLine($"Storage Path: {_storageDirectory}");
    }

    public async Task<string> GetStoragePathAsync()
    {
        return await Task.FromResult(_storageDirectory);
    }

    private string GetFullFilePath()
    {
        return Path.Combine(_storageDirectory, _customItemsFile);
    }

    public async Task<List<Item>> LoadCustomItemsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine("No custom items file found, returning empty list.");
            return new List<Item>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var items = JsonSerializer.Deserialize<List<Item>>(json, _jsonOptions);
            return items ?? new List<Item>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom items: {ex.Message}");
            return new List<Item>();
        }
    }

    public async Task SaveCustomItemAsync(Item item)
    {
        var items = await LoadCustomItemsAsync();

        // Check if item already exists (update) or add new
        var existing = items.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            var index = items.IndexOf(existing);
            items[index] = item;
        }
        else
        {
            items.Add(item);
        }

        await SaveCustomItemsAsync(items);
    }

    public async Task SaveCustomItemsAsync(List<Item> items)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(items, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            System.Diagnostics.Debug.WriteLine($"Saved {items.Count} custom items to {filePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom items: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomItemAsync(int id)
    {
        var items = await LoadCustomItemsAsync();
        var item = items.FirstOrDefault(i => i.Id == id);
        if (item == null) return false;

        items.Remove(item);
        await SaveCustomItemsAsync(items);
        return true;
    }
}