using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CustomQuipStorageService : ICustomQuipStorageService
{
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _customQuipsFile = "custom_quips.json";

    public CustomQuipStorageService()
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
        return Path.Combine(_storageDirectory, _customQuipsFile);
    }

    public async Task<List<Quip>> LoadCustomQuipsAsync()
    {
        var filePath = GetFullFilePath();

        if (!File.Exists(filePath))
        {
            return new List<Quip>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var quips = JsonSerializer.Deserialize<List<Quip>>(json, _jsonOptions);
            return quips ?? new List<Quip>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading custom quips: {ex.Message}");
            return new List<Quip>();
        }
    }

    public async Task SaveCustomQuipAsync(Quip quip)
    {
        var quips = await LoadCustomQuipsAsync();

        var existing = quips.FirstOrDefault(q => q.Guid == quip.Guid);
        if (existing != null)
        {
            var index = quips.IndexOf(existing);
            quips[index] = quip;
        }
        else
        {
            quips.Add(quip);
        }

        await SaveCustomQuipsAsync(quips);
    }

    public async Task SaveCustomQuipsAsync(List<Quip> quips)
    {
        try
        {
            var filePath = GetFullFilePath();
            string json = JsonSerializer.Serialize(quips, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving custom quips: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteCustomQuipAsync(Guid guid)
    {
        var quips = await LoadCustomQuipsAsync();
        var quip = quips.FirstOrDefault(q => q.Guid == guid);
        if (quip == null) return false;

        quips.Remove(quip);
        await SaveCustomQuipsAsync(quips);
        return true;
    }
}