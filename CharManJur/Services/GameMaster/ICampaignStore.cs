using CharManJur.Models;

namespace CharManJur.Services;

public interface ICampaignStore
{
    Task<List<Campaign>> GetCampaignsAsync();
    Task<Campaign?> LoadCampaignAsync(Guid campaignId);
    Task<string?> SaveCampaignAsync(Campaign campaign);          // the campaign creator: validates, returns an error or null when saved
    Task SaveStateAsync(Campaign campaign);                      // the running session (day, time, password, policy): saved as it is
    Task<bool> DeleteCampaignAsync(Guid campaignId);

    // Characters are cloned into the campaign when the session closes; a character rejoining is checked against its clone.
    Task SnapshotCharactersAsync(Campaign campaign, IEnumerable<CharacterSaveData> characters);
    Task<CharacterSaveData?> GetSnapshotAsync(Campaign campaign, Guid characterGuid);
    Task<TamperReport> ValidateCharacterAsync(Campaign campaign, CharacterSaveData current);

    string CampaignFolder(Guid campaignId);   // where the campaign's files (event log, snapshots) live
    string PadFolder(Guid campaignId);        // the GM's scratch pad
    string MapsFolder(Guid campaignId);       // the GM's maps
    string SymbolsFolder(Guid campaignId);    // the symbol library
}