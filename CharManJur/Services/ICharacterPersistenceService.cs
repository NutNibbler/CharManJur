using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICharacterPersistenceService
{
    Task<bool> SaveCharacterAsync(string playerName, string characterName);
    Task<CharacterSaveData?> LoadCharacterAsync(string fileName);
    Task<List<CharacterSaveData>> GetAllSavedCharactersAsync();
    Task<bool> DeleteCharacterAsync(string fileName);
    Task<bool> CharacterExistsAsync(string fileName);
    Task<string> GenerateFileName(string playerName, string characterName);

    // NEW: Save with full data object
    Task<bool> SaveCharacterDataAsync(CharacterSaveData saveData);
}