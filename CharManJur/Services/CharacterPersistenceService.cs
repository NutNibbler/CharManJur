using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CharManJur.Models;

namespace CharManJur.Services;

public class CharacterPersistenceService : ICharacterPersistenceService
{
    private readonly string _saveDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public CharacterPersistenceService()
    {
        // Save to a dedicated folder in the app's local storage
        string appDataPath = FileSystem.AppDataDirectory;
        _saveDirectory = Path.Combine(appDataPath, "Characters");

        System.Diagnostics.Debug.WriteLine($"========================================");
        System.Diagnostics.Debug.WriteLine($"CHARACTER PERSISTENCE SERVICE INITIALIZED");
        System.Diagnostics.Debug.WriteLine($"========================================");
        System.Diagnostics.Debug.WriteLine($"AppData Directory: {appDataPath}");
        System.Diagnostics.Debug.WriteLine($"Save Directory: {_saveDirectory}");

        // Create directory if it doesn't exist
        try
        {
            if (!Directory.Exists(_saveDirectory))
            {
                System.Diagnostics.Debug.WriteLine($"Directory does NOT exist. Creating...");
                Directory.CreateDirectory(_saveDirectory);
                System.Diagnostics.Debug.WriteLine($"Directory created successfully.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Directory already exists.");
            }

            // Verify directory exists
            System.Diagnostics.Debug.WriteLine($"Directory exists: {Directory.Exists(_saveDirectory)}");

            // List any existing files
            var existingFiles = Directory.GetFiles(_saveDirectory, "*.json");
            System.Diagnostics.Debug.WriteLine($"Existing JSON files: {existingFiles.Length}");
            foreach (var file in existingFiles)
            {
                System.Diagnostics.Debug.WriteLine($"  - {Path.GetFileName(file)}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR creating directory: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        System.Diagnostics.Debug.WriteLine($"========================================");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> GenerateFileName(string playerName, string characterName)
    {
        // Sanitize names for file system
        string safePlayer = string.Join("_", playerName.Split(Path.GetInvalidFileNameChars()));
        string safeCharacter = string.Join("_", characterName.Split(Path.GetInvalidFileNameChars()));

        // If names are empty, use defaults
        if (string.IsNullOrWhiteSpace(safePlayer)) safePlayer = "UnknownPlayer";
        if (string.IsNullOrWhiteSpace(safeCharacter)) safeCharacter = "UnknownCharacter";

        string fileName = $"{safePlayer}_{safeCharacter}.json";
        System.Diagnostics.Debug.WriteLine($"Generated filename: {fileName}");
        return fileName;
    }

    public async Task<bool> SaveCharacterDataAsync(CharacterSaveData saveData)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"SAVING CHARACTER DATA");
            System.Diagnostics.Debug.WriteLine($"========================================");

            if (saveData == null)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: saveData is null!");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"FileName: {saveData.FileName}");
            System.Diagnostics.Debug.WriteLine($"PlayerName: {saveData.PlayerName}");
            System.Diagnostics.Debug.WriteLine($"CharacterName: {saveData.CharacterName}");
            System.Diagnostics.Debug.WriteLine($"IsComplete: {saveData.IsComplete}");
            System.Diagnostics.Debug.WriteLine($"LastSaved: {saveData.LastSaved}");

            string fullPath = Path.Combine(_saveDirectory, saveData.FileName);
            System.Diagnostics.Debug.WriteLine($"Full Path: {fullPath}");
            System.Diagnostics.Debug.WriteLine($"Directory exists: {Directory.Exists(_saveDirectory)}");

            // Ensure directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                System.Diagnostics.Debug.WriteLine($"Directory does not exist! Creating...");
                Directory.CreateDirectory(_saveDirectory);
            }

            string json = JsonSerializer.Serialize(saveData, _jsonOptions);
            System.Diagnostics.Debug.WriteLine($"JSON serialized. Length: {json.Length} bytes");

            await File.WriteAllTextAsync(fullPath, json);

            System.Diagnostics.Debug.WriteLine($"FILE SAVED SUCCESSFULLY!");

            // Verify file was created
            if (File.Exists(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                System.Diagnostics.Debug.WriteLine($"File exists. Size: {fileInfo.Length} bytes");
                System.Diagnostics.Debug.WriteLine($"File created at: {fileInfo.CreationTime}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"WARNING: File does not exist after save attempt!");
            }

            System.Diagnostics.Debug.WriteLine($"========================================");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"ERROR SAVING CHARACTER");
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            System.Diagnostics.Debug.WriteLine($"========================================");
            return false;
        }
    }

    public async Task<bool> SaveCharacterAsync(string playerName, string characterName)
    {
        try
        {
            string fileName = await GenerateFileName(playerName, characterName);
            string fullPath = Path.Combine(_saveDirectory, fileName);

            var saveData = new CharacterSaveData
            {
                FileName = fileName,
                PlayerName = playerName,
                CharacterName = characterName,
                LastSaved = DateTime.Now,
                IsComplete = false,
                IsSaved = true,
                Data = new CharacterData()
            };

            string json = JsonSerializer.Serialize(saveData, _jsonOptions);
            await File.WriteAllTextAsync(fullPath, json);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
            return false;
        }
    }

    public async Task<CharacterSaveData?> LoadCharacterAsync(string fileName)
    {
        try
        {
            string fullPath = Path.Combine(_saveDirectory, fileName);

            if (!File.Exists(fullPath))
                return null;

            string json = await File.ReadAllTextAsync(fullPath);
            return JsonSerializer.Deserialize<CharacterSaveData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<CharacterSaveData>> GetAllSavedCharactersAsync()
    {
        var result = new List<CharacterSaveData>();

        try
        {
            System.Diagnostics.Debug.WriteLine($"=== GETTING ALL CHARACTERS ===");
            System.Diagnostics.Debug.WriteLine($"Directory: {_saveDirectory}");
            System.Diagnostics.Debug.WriteLine($"Directory exists: {Directory.Exists(_saveDirectory)}");

            if (!Directory.Exists(_saveDirectory))
            {
                System.Diagnostics.Debug.WriteLine($"Directory does not exist! Creating...");
                Directory.CreateDirectory(_saveDirectory);
                return result;
            }

            var files = Directory.GetFiles(_saveDirectory, "*.json");
            System.Diagnostics.Debug.WriteLine($"Found {files.Length} JSON files");

            foreach (var file in files)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file);
                    var data = JsonSerializer.Deserialize<CharacterSaveData>(json, _jsonOptions);
                    if (data != null)
                    {
                        result.Add(data);
                        System.Diagnostics.Debug.WriteLine($"Loaded: {data.CharacterName} ({data.FileName})");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error reading {file}: {ex.Message}");
                }
            }

            result = result.OrderByDescending(r => r.LastSaved).ToList();
            System.Diagnostics.Debug.WriteLine($"Returning {result.Count} characters");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get all error: {ex.Message}");
        }

        return result;
    }

    public async Task<bool> DeleteCharacterAsync(string fileName)
    {
        try
        {
            string fullPath = Path.Combine(_saveDirectory, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Delete error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CharacterExistsAsync(string fileName)
    {
        string fullPath = Path.Combine(_saveDirectory, fileName);
        return File.Exists(fullPath);
    }
}