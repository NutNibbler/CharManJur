using CharManJur.Models;

namespace CharManJur.Services;

// Character persistence as seen by the GM's Player Viewer: loading works, saving and deleting do nothing,
// so no screen in the viewer can overwrite a player's file.
public sealed class ReadOnlyPersistence : ICharacterPersistenceService
{
    private readonly ICharacterPersistenceService _inner;

    public ReadOnlyPersistence(ICharacterPersistenceService inner) => _inner = inner;

    public Task<string> GenerateFileName(string playerName, string characterName)
        => _inner.GenerateFileName(playerName, characterName);

    public Task<CharacterSaveData?> LoadCharacterAsync(string fileName) => _inner.LoadCharacterAsync(fileName);

    public Task<List<CharacterSaveData>> GetAllSavedCharactersAsync() => _inner.GetAllSavedCharactersAsync();

    public Task<bool> CharacterExistsAsync(string fileName) => _inner.CharacterExistsAsync(fileName);

    // Reported as done, so nothing shows a "save failed" message, but nothing is written.
    public Task<bool> SaveCharacterDataAsync(CharacterSaveData saveData) => Task.FromResult(true);

    public Task<bool> SaveCharacterAsync(string playerName, string characterName) => Task.FromResult(true);

    public Task<bool> DeleteCharacterAsync(string fileName) => Task.FromResult(false);
}