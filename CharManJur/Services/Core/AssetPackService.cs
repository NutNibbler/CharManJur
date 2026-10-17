using System.Text.Json;
using CharManJur.Models;

namespace CharManJur.Services;

public class AssetPackService : IAssetPackService
{
    private readonly IItemDataService _itemDataService;
    private readonly ICustomItemStorageService _customItemStorageService;
    private readonly IBackgroundDataService _backgroundDataService;
    private readonly ICustomBackgroundStorageService _customBackgroundStorageService;
    private readonly IFamiliarDataService _familiarDataService;
    private readonly ICustomFamiliarStorageService _customFamiliarStorageService;
    private readonly IRaceDataService _raceDataService;
    private readonly ICustomRaceStorageService _customRaceStorageService;
    private readonly IHinderanceDataService _hinderanceDataService;
    private readonly ICustomHinderanceStorageService _customHinderanceStorageService;
    private readonly IBlueprintDataService _blueprintDataService;
    private readonly ICustomBlueprintStorageService _customBlueprintStorageService;
    private readonly ISpellDataService _spellDataService;
    private readonly ICustomSpellStorageService _customSpellStorageService;
    private readonly IQuipDataService _quipDataService;
    private readonly ICustomQuipStorageService _customQuipStorageService;
    private readonly ITechniqueDataService _techniqueDataService;
    private readonly ICustomTechniqueStorageService _customTechniqueStorageService;
    private readonly ITraitDataService _traitDataService;
    private readonly ICustomTraitStorageService _customTraitStorageService;
    private readonly string _packsDirectory;
    private readonly string _indexFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AssetPackService(
        IItemDataService itemDataService,
        ICustomItemStorageService customItemStorageService,
        IBackgroundDataService backgroundDataService,
        ICustomBackgroundStorageService customBackgroundStorageService,
        IFamiliarDataService familiarDataService,
        ICustomFamiliarStorageService customFamiliarStorageService,
        IRaceDataService raceDataService,
        ICustomRaceStorageService customRaceStorageService,
        IHinderanceDataService hinderanceDataService,
        ICustomHinderanceStorageService customHinderanceStorageService,
        IBlueprintDataService blueprintDataService,
        ICustomBlueprintStorageService customBlueprintStorageService,
        ISpellDataService spellDataService,
        ICustomSpellStorageService customSpellStorageService,
        IQuipDataService quipDataService,
        ICustomQuipStorageService customQuipStorageService,
        ITechniqueDataService techniqueDataService,
        ICustomTechniqueStorageService customTechniqueStorageService,
        ITraitDataService traitDataService,
        ICustomTraitStorageService customTraitStorageService)
    {
        _itemDataService = itemDataService;
        _customItemStorageService = customItemStorageService;
        _backgroundDataService = backgroundDataService;
        _customBackgroundStorageService = customBackgroundStorageService;
        _familiarDataService = familiarDataService;
        _customFamiliarStorageService = customFamiliarStorageService;
        _raceDataService = raceDataService;
        _customRaceStorageService = customRaceStorageService;
        _hinderanceDataService = hinderanceDataService;
        _customHinderanceStorageService = customHinderanceStorageService;
        _blueprintDataService = blueprintDataService;
        _customBlueprintStorageService = customBlueprintStorageService;
        _spellDataService = spellDataService;
        _customSpellStorageService = customSpellStorageService;
        _quipDataService = quipDataService;
        _customQuipStorageService = customQuipStorageService;
        _techniqueDataService = techniqueDataService;
        _customTechniqueStorageService = customTechniqueStorageService;
        _traitDataService = traitDataService;
        _customTraitStorageService = customTraitStorageService;

        string appDataPath = AppPaths.DataDirectory;
        _packsDirectory = Path.Combine(appDataPath, "AssetPacks");
        _indexFilePath = Path.Combine(_packsDirectory, "InstalledPacks.json");

        if (!Directory.Exists(_packsDirectory))
        {
            Directory.CreateDirectory(_packsDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<string> ExportPackAsync(
        string packName, string? author,
        List<Guid> itemGuidsToInclude, List<Guid> backgroundGuidsToInclude, List<Guid> familiarGuidsToInclude,
        List<Guid> raceGuidsToInclude, List<Guid> hinderanceGuidsToInclude,
        List<Guid> blueprintGuidsToInclude, List<Guid> spellGuidsToInclude, List<Guid> quipGuidsToInclude, List<Guid> techniqueGuidsToInclude,
        bool moveItems, List<Guid>? traitGuidsToInclude = null)
    {
        traitGuidsToInclude ??= new List<Guid>();

        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        var itemsToExport = allCustomItems.Where(i => itemGuidsToInclude.Contains(i.Guid)).ToList();

        var allCustomBackgrounds = await _backgroundDataService.GetCustomBackgroundsAsync();
        var backgroundsToExport = allCustomBackgrounds.Where(b => backgroundGuidsToInclude.Contains(b.Guid)).ToList();

        var allCustomFamiliars = await _familiarDataService.GetCustomFamiliarsAsync();
        var familiarsToExport = allCustomFamiliars.Where(f => familiarGuidsToInclude.Contains(f.Guid)).ToList();

        var allCustomRaces = await _raceDataService.GetCustomRacesAsync();
        var racesToExport = allCustomRaces.Where(r => raceGuidsToInclude.Contains(r.Guid)).ToList();

        var allCustomHinderances = await _hinderanceDataService.GetCustomHinderancesAsync();
        var hinderancesToExport = allCustomHinderances.Where(h => hinderanceGuidsToInclude.Contains(h.Guid)).ToList();

        var allCustomBlueprints = await _blueprintDataService.GetCustomBlueprintsAsync();
        var blueprintsToExport = allCustomBlueprints.Where(bp => blueprintGuidsToInclude.Contains(bp.Guid)).ToList();

        var allCustomSpells = await _spellDataService.GetCustomSpellsAsync();
        var spellsToExport = allCustomSpells.Where(s => spellGuidsToInclude.Contains(s.Guid)).ToList();

        var allCustomQuips = await _quipDataService.GetCustomQuipsAsync();
        var quipsToExport = allCustomQuips.Where(q => quipGuidsToInclude.Contains(q.Guid)).ToList();

        var allCustomTechniques = await _techniqueDataService.GetCustomTechniquesAsync();
        var techniquesToExport = allCustomTechniques.Where(t => techniqueGuidsToInclude.Contains(t.Guid)).ToList();

        var allCustomTraits = await _traitDataService.GetCustomTraitsAsync();
        var traitsToExport = allCustomTraits.Where(tr => traitGuidsToInclude.Contains(tr.Guid)).ToList();

        var pack = new AssetPack
        {
            Name = packName,
            Author = author,
            Items = itemsToExport,
            Backgrounds = backgroundsToExport,
            Familiars = familiarsToExport,
            Races = racesToExport,
            Hinderances = hinderancesToExport,
            Blueprints = blueprintsToExport,
            Spells = spellsToExport,
            Quips = quipsToExport,
            Techniques = techniquesToExport,
            Traits = traitsToExport
        };

        if (moveItems)
        {
            foreach (var item in itemsToExport) item.SourcePackId = pack.PackId;
            foreach (var background in backgroundsToExport) background.SourcePackId = pack.PackId;
            foreach (var familiar in familiarsToExport) familiar.SourcePackId = pack.PackId;
            foreach (var race in racesToExport) race.SourcePackId = pack.PackId;
            foreach (var hinderance in hinderancesToExport) hinderance.SourcePackId = pack.PackId;
            foreach (var blueprint in blueprintsToExport) blueprint.SourcePackId = pack.PackId;
            foreach (var spell in spellsToExport) spell.SourcePackId = pack.PackId;
            foreach (var quip in quipsToExport) quip.SourcePackId = pack.PackId;
            foreach (var technique in techniquesToExport) technique.SourcePackId = pack.PackId;
            foreach (var trait in traitsToExport) trait.SourcePackId = pack.PackId;
        }

        string safeName = string.Join("_", packName.Split(Path.GetInvalidFileNameChars()));
        string fileName = $"{safeName}_{pack.PackId.Substring(0, 8)}.json";
        string filePath = Path.Combine(_packsDirectory, fileName);

        string json = JsonSerializer.Serialize(pack, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        var index = await LoadIndexAsync();
        if (!index.Any(p => p.PackId == pack.PackId))
        {
            index.Add(new InstalledPackEntry
            {
                PackId = pack.PackId,
                Name = pack.Name,
                FileName = fileName,
                IsLoaded = false // a freshly saved pack starts unloaded — you load it explicitly when you want it active
            });
            await SaveIndexAsync(index);
        }

        if (moveItems)
        {
            var storedItems = await _customItemStorageService.LoadCustomItemsAsync();
            foreach (var guid in itemGuidsToInclude)
            {
                var stored = storedItems.FirstOrDefault(i => i.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customItemStorageService.SaveCustomItemsAsync(storedItems);
            foreach (var item in itemsToExport) { item.IsLoaded = false; item.Id = 0; }

            var storedBackgrounds = await _customBackgroundStorageService.LoadCustomBackgroundsAsync();
            foreach (var guid in backgroundGuidsToInclude)
            {
                var stored = storedBackgrounds.FirstOrDefault(b => b.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customBackgroundStorageService.SaveCustomBackgroundsAsync(storedBackgrounds);
            foreach (var background in backgroundsToExport) { background.IsLoaded = false; background.Id = 0; }

            var storedFamiliars = await _customFamiliarStorageService.LoadCustomFamiliarsAsync();
            foreach (var guid in familiarGuidsToInclude)
            {
                var stored = storedFamiliars.FirstOrDefault(f => f.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customFamiliarStorageService.SaveCustomFamiliarsAsync(storedFamiliars);
            foreach (var familiar in familiarsToExport) { familiar.IsLoaded = false; familiar.Id = 0; }

            var storedRaces = await _customRaceStorageService.LoadCustomRacesAsync();
            foreach (var guid in raceGuidsToInclude)
            {
                var stored = storedRaces.FirstOrDefault(r => r.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customRaceStorageService.SaveCustomRacesAsync(storedRaces);
            foreach (var race in racesToExport) { race.IsLoaded = false; race.Id = 0; }

            var storedHinderances = await _customHinderanceStorageService.LoadCustomHinderancesAsync();
            foreach (var guid in hinderanceGuidsToInclude)
            {
                var stored = storedHinderances.FirstOrDefault(h => h.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customHinderanceStorageService.SaveCustomHinderancesAsync(storedHinderances);
            foreach (var hinderance in hinderancesToExport) { hinderance.IsLoaded = false; hinderance.Id = 0; }

            var storedBlueprints = await _customBlueprintStorageService.LoadCustomBlueprintsAsync();
            foreach (var guid in blueprintGuidsToInclude)
            {
                var stored = storedBlueprints.FirstOrDefault(bp => bp.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customBlueprintStorageService.SaveCustomBlueprintsAsync(storedBlueprints);
            foreach (var blueprint in blueprintsToExport) { blueprint.IsLoaded = false; blueprint.Id = 0; }

            var storedSpells = await _customSpellStorageService.LoadCustomSpellsAsync();
            foreach (var guid in spellGuidsToInclude)
            {
                var stored = storedSpells.FirstOrDefault(s => s.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customSpellStorageService.SaveCustomSpellsAsync(storedSpells);
            foreach (var spell in spellsToExport) { spell.IsLoaded = false; spell.Id = 0; }

            var storedQuips = await _customQuipStorageService.LoadCustomQuipsAsync();
            foreach (var guid in quipGuidsToInclude)
            {
                var stored = storedQuips.FirstOrDefault(q => q.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customQuipStorageService.SaveCustomQuipsAsync(storedQuips);
            foreach (var quip in quipsToExport) { quip.IsLoaded = false; quip.Id = 0; }

            var storedTechniques = await _customTechniqueStorageService.LoadCustomTechniquesAsync();
            foreach (var guid in techniqueGuidsToInclude)
            {
                var stored = storedTechniques.FirstOrDefault(t => t.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customTechniqueStorageService.SaveCustomTechniquesAsync(storedTechniques);
            foreach (var technique in techniquesToExport) { technique.IsLoaded = false; technique.Id = 0; }

            var storedTraits = await _customTraitStorageService.LoadCustomTraitsAsync();
            foreach (var guid in traitGuidsToInclude)
            {
                var stored = storedTraits.FirstOrDefault(tr => tr.Guid == guid);
                if (stored != null) { stored.SourcePackId = pack.PackId; stored.IsLoaded = false; }
            }
            await _customTraitStorageService.SaveCustomTraitsAsync(storedTraits);
            foreach (var trait in traitsToExport) trait.IsLoaded = false;
        }

        return filePath;
    }

    public async Task<(bool Success, string Message)> ImportPackAsync(string filePath)
    {
        string json;
        try { json = await File.ReadAllTextAsync(filePath); }
        catch (Exception ex) { return (false, $"Could not read file: {ex.Message}"); }

        AssetPack? pack;
        try { pack = JsonSerializer.Deserialize<AssetPack>(json, _jsonOptions); }
        catch (Exception ex) { return (false, $"Invalid pack file: {ex.Message}"); }

        if (pack == null || (pack.Items.Count == 0 && pack.Backgrounds.Count == 0 && pack.Familiars.Count == 0 &&
            pack.Races.Count == 0 && pack.Hinderances.Count == 0 && pack.Blueprints.Count == 0 &&
            pack.Spells.Count == 0 && pack.Quips.Count == 0 && pack.Techniques.Count == 0 && pack.Traits.Count == 0))
        {
            return (false, "Pack file contained no assets.");
        }

        var existingCustomItems = await _customItemStorageService.LoadCustomItemsAsync();
        int itemsOverwritten = 0, itemsAdded = 0;
        foreach (var incoming in pack.Items)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomItems.FirstOrDefault(i => i.Guid == incoming.Guid);
            if (existing != null) { existingCustomItems.Remove(existing); itemsOverwritten++; } else { itemsAdded++; }
            existingCustomItems.Add(incoming);
        }
        await _customItemStorageService.SaveCustomItemsAsync(existingCustomItems);

        var existingCustomBackgrounds = await _customBackgroundStorageService.LoadCustomBackgroundsAsync();
        int backgroundsOverwritten = 0, backgroundsAdded = 0;
        foreach (var incoming in pack.Backgrounds)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            var existing = existingCustomBackgrounds.FirstOrDefault(b => b.Guid == incoming.Guid);
            if (existing != null) { existingCustomBackgrounds.Remove(existing); backgroundsOverwritten++; } else { backgroundsAdded++; }
            existingCustomBackgrounds.Add(incoming);
        }
        await _customBackgroundStorageService.SaveCustomBackgroundsAsync(existingCustomBackgrounds);

        var existingCustomFamiliars = await _customFamiliarStorageService.LoadCustomFamiliarsAsync();
        int familiarsOverwritten = 0, familiarsAdded = 0;
        foreach (var incoming in pack.Familiars)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            incoming.IsFoundation = false;
            var existing = existingCustomFamiliars.FirstOrDefault(f => f.Guid == incoming.Guid);
            if (existing != null) { existingCustomFamiliars.Remove(existing); familiarsOverwritten++; } else { familiarsAdded++; }
            existingCustomFamiliars.Add(incoming);
        }
        await _customFamiliarStorageService.SaveCustomFamiliarsAsync(existingCustomFamiliars);

        var existingCustomRaces = await _customRaceStorageService.LoadCustomRacesAsync();
        int racesOverwritten = 0, racesAdded = 0;
        foreach (var incoming in pack.Races)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            var existing = existingCustomRaces.FirstOrDefault(r => r.Guid == incoming.Guid);
            if (existing != null) { existingCustomRaces.Remove(existing); racesOverwritten++; } else { racesAdded++; }
            existingCustomRaces.Add(incoming);
        }
        await _customRaceStorageService.SaveCustomRacesAsync(existingCustomRaces);

        var existingCustomHinderances = await _customHinderanceStorageService.LoadCustomHinderancesAsync();
        int hinderancesOverwritten = 0, hinderancesAdded = 0;
        foreach (var incoming in pack.Hinderances)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomHinderances.FirstOrDefault(h => h.Guid == incoming.Guid);
            if (existing != null) { existingCustomHinderances.Remove(existing); hinderancesOverwritten++; } else { hinderancesAdded++; }
            existingCustomHinderances.Add(incoming);
        }
        await _customHinderanceStorageService.SaveCustomHinderancesAsync(existingCustomHinderances);

        var existingCustomBlueprints = await _customBlueprintStorageService.LoadCustomBlueprintsAsync();
        int blueprintsOverwritten = 0, blueprintsAdded = 0;
        foreach (var incoming in pack.Blueprints)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomBlueprints.FirstOrDefault(bp => bp.Guid == incoming.Guid);
            if (existing != null) { existingCustomBlueprints.Remove(existing); blueprintsOverwritten++; } else { blueprintsAdded++; }
            existingCustomBlueprints.Add(incoming);
        }
        await _customBlueprintStorageService.SaveCustomBlueprintsAsync(existingCustomBlueprints);

        var existingCustomSpells = await _customSpellStorageService.LoadCustomSpellsAsync();
        int spellsOverwritten = 0, spellsAdded = 0;
        foreach (var incoming in pack.Spells)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomSpells.FirstOrDefault(s => s.Guid == incoming.Guid);
            if (existing != null) { existingCustomSpells.Remove(existing); spellsOverwritten++; } else { spellsAdded++; }
            existingCustomSpells.Add(incoming);
        }
        await _customSpellStorageService.SaveCustomSpellsAsync(existingCustomSpells);

        var existingCustomQuips = await _customQuipStorageService.LoadCustomQuipsAsync();
        int quipsOverwritten = 0, quipsAdded = 0;
        foreach (var incoming in pack.Quips)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomQuips.FirstOrDefault(q => q.Guid == incoming.Guid);
            if (existing != null) { existingCustomQuips.Remove(existing); quipsOverwritten++; } else { quipsAdded++; }
            existingCustomQuips.Add(incoming);
        }
        await _customQuipStorageService.SaveCustomQuipsAsync(existingCustomQuips);

        var existingCustomTechniques = await _customTechniqueStorageService.LoadCustomTechniquesAsync();
        int techniquesOverwritten = 0, techniquesAdded = 0;
        foreach (var incoming in pack.Techniques)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomTechniques.FirstOrDefault(t => t.Guid == incoming.Guid);
            if (existing != null) { existingCustomTechniques.Remove(existing); techniquesOverwritten++; } else { techniquesAdded++; }
            existingCustomTechniques.Add(incoming);
        }
        await _customTechniqueStorageService.SaveCustomTechniquesAsync(existingCustomTechniques);

        var existingCustomTraits = await _customTraitStorageService.LoadCustomTraitsAsync();
        int traitsOverwritten = 0, traitsAdded = 0;
        foreach (var incoming in pack.Traits)
        {
            incoming.SourcePackId = pack.PackId;
            incoming.IsLoaded = true;
            incoming.IsPlayerCreated = true;
            var existing = existingCustomTraits.FirstOrDefault(tr => tr.Guid == incoming.Guid);
            if (existing != null) { existingCustomTraits.Remove(existing); traitsOverwritten++; } else { traitsAdded++; }
            existingCustomTraits.Add(incoming);
        }
        await _customTraitStorageService.SaveCustomTraitsAsync(existingCustomTraits);

        string safeName = string.Join("_", pack.Name.Split(Path.GetInvalidFileNameChars()));
        string fileName = $"{safeName}_{pack.PackId.Substring(0, 8)}.json";
        string destPath = Path.Combine(_packsDirectory, fileName);
        if (Path.GetFullPath(filePath) != Path.GetFullPath(destPath))
        {
            File.Copy(filePath, destPath, overwrite: true);
        }

        var index = await LoadIndexAsync();
        var existingEntry = index.FirstOrDefault(p => p.PackId == pack.PackId);
        if (existingEntry != null) { existingEntry.IsLoaded = true; }
        else
        {
            index.Add(new InstalledPackEntry { PackId = pack.PackId, Name = pack.Name, FileName = fileName, IsLoaded = true });
        }
        await SaveIndexAsync(index);

        int totalOverwritten = itemsOverwritten + backgroundsOverwritten + familiarsOverwritten + racesOverwritten +
            hinderancesOverwritten + blueprintsOverwritten + spellsOverwritten + quipsOverwritten + techniquesOverwritten + traitsOverwritten;

        string message = totalOverwritten > 0
            ? $"Imported '{pack.Name}': {itemsAdded} item(s), {backgroundsAdded} background(s), {familiarsAdded} familiar(s), {racesAdded} race(s), {hinderancesAdded} hinderance(s), {blueprintsAdded} blueprint(s), {spellsAdded} spell(s), {quipsAdded} quip(s), {techniquesAdded} technique(s), {traitsAdded} trait(s) added; {totalOverwritten} existing asset(s) overwritten."
            : $"Imported '{pack.Name}': {itemsAdded} item(s), {backgroundsAdded} background(s), {familiarsAdded} familiar(s), {racesAdded} race(s), {hinderancesAdded} hinderance(s), {blueprintsAdded} blueprint(s), {spellsAdded} spell(s), {quipsAdded} quip(s), {techniquesAdded} technique(s), {traitsAdded} trait(s).";

        return (true, message);
    }

    public async Task<List<InstalledPackEntry>> GetInstalledPacksAsync()
    {
        return await LoadIndexAsync();
    }

    public async Task SetPackLoadedAsync(string packId, bool isLoaded)
    {
        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        foreach (var item in allCustomItems.Where(i => i.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _itemDataService.LoadItemAsync(item.Guid); else await _itemDataService.UnloadItemAsync(item.Guid);
        }

        var allCustomBackgrounds = await _backgroundDataService.GetCustomBackgroundsAsync();
        foreach (var background in allCustomBackgrounds.Where(b => b.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _backgroundDataService.LoadBackgroundAsync(background.Guid); else await _backgroundDataService.UnloadBackgroundAsync(background.Guid);
        }

        var allCustomFamiliars = await _familiarDataService.GetCustomFamiliarsAsync();
        foreach (var familiar in allCustomFamiliars.Where(f => f.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _familiarDataService.LoadFamiliarAsync(familiar.Guid); else await _familiarDataService.UnloadFamiliarAsync(familiar.Guid);
        }

        var allCustomRaces = await _raceDataService.GetCustomRacesAsync();
        foreach (var race in allCustomRaces.Where(r => r.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _raceDataService.LoadRaceAsync(race.Guid); else await _raceDataService.UnloadRaceAsync(race.Guid);
        }

        var allCustomHinderances = await _hinderanceDataService.GetCustomHinderancesAsync();
        foreach (var hinderance in allCustomHinderances.Where(h => h.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _hinderanceDataService.LoadHinderanceAsync(hinderance.Guid); else await _hinderanceDataService.UnloadHinderanceAsync(hinderance.Guid);
        }

        var allCustomBlueprints = await _blueprintDataService.GetCustomBlueprintsAsync();
        foreach (var blueprint in allCustomBlueprints.Where(bp => bp.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _blueprintDataService.LoadBlueprintAsync(blueprint.Guid); else await _blueprintDataService.UnloadBlueprintAsync(blueprint.Guid);
        }

        var allCustomSpells = await _spellDataService.GetCustomSpellsAsync();
        foreach (var spell in allCustomSpells.Where(s => s.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _spellDataService.LoadSpellAsync(spell.Guid); else await _spellDataService.UnloadSpellAsync(spell.Guid);
        }

        var allCustomQuips = await _quipDataService.GetCustomQuipsAsync();
        foreach (var quip in allCustomQuips.Where(q => q.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _quipDataService.LoadQuipAsync(quip.Guid); else await _quipDataService.UnloadQuipAsync(quip.Guid);
        }

        var allCustomTechniques = await _techniqueDataService.GetCustomTechniquesAsync();
        foreach (var technique in allCustomTechniques.Where(t => t.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _techniqueDataService.LoadTechniqueAsync(technique.Guid); else await _techniqueDataService.UnloadTechniqueAsync(technique.Guid);
        }

        var allCustomTraits = await _traitDataService.GetCustomTraitsAsync();
        foreach (var trait in allCustomTraits.Where(tr => tr.SourcePackId == packId).ToList())
        {
            if (isLoaded) await _traitDataService.LoadTraitAsync(trait.Guid); else await _traitDataService.UnloadTraitAsync(trait.Guid);
        }

        var index = await LoadIndexAsync();
        var entry = index.FirstOrDefault(p => p.PackId == packId);
        if (entry != null) { entry.IsLoaded = isLoaded; await SaveIndexAsync(index); }
    }

    public async Task<(bool Success, string Message)> DeletePackAsync(string packId)
    {
        if (packId == "Local")
        {
            return (false, "The Local pack can't be deleted.");
        }

        // ToList() on each loop: some data services hand back their live list, and deleting while enumerating it throws.
        var allCustomItems = await _itemDataService.GetCustomItemsAsync();
        foreach (var item in allCustomItems.Where(i => i.SourcePackId == packId).ToList()) await _itemDataService.DeleteItemAsync(item.Guid);

        var allCustomBackgrounds = await _backgroundDataService.GetCustomBackgroundsAsync();
        foreach (var background in allCustomBackgrounds.Where(b => b.SourcePackId == packId).ToList()) await _backgroundDataService.DeleteBackgroundAsync(background.Guid);

        var allCustomFamiliars = await _familiarDataService.GetCustomFamiliarsAsync();
        foreach (var familiar in allCustomFamiliars.Where(f => f.SourcePackId == packId).ToList()) await _familiarDataService.DeleteFamiliarAsync(familiar.Guid);

        var allCustomRaces = await _raceDataService.GetCustomRacesAsync();
        foreach (var race in allCustomRaces.Where(r => r.SourcePackId == packId).ToList()) await _raceDataService.DeleteRaceAsync(race.Guid);

        var allCustomHinderances = await _hinderanceDataService.GetCustomHinderancesAsync();
        foreach (var hinderance in allCustomHinderances.Where(h => h.SourcePackId == packId).ToList()) await _hinderanceDataService.DeleteHinderanceAsync(hinderance.Guid);

        var allCustomBlueprints = await _blueprintDataService.GetCustomBlueprintsAsync();
        foreach (var blueprint in allCustomBlueprints.Where(bp => bp.SourcePackId == packId).ToList()) await _blueprintDataService.DeleteBlueprintAsync(blueprint.Guid);

        var allCustomSpells = await _spellDataService.GetCustomSpellsAsync();
        foreach (var spell in allCustomSpells.Where(s => s.SourcePackId == packId).ToList()) await _spellDataService.DeleteSpellAsync(spell.Guid);

        var allCustomQuips = await _quipDataService.GetCustomQuipsAsync();
        foreach (var quip in allCustomQuips.Where(q => q.SourcePackId == packId).ToList()) await _quipDataService.DeleteQuipAsync(quip.Guid);

        var allCustomTechniques = await _techniqueDataService.GetCustomTechniquesAsync();
        foreach (var technique in allCustomTechniques.Where(t => t.SourcePackId == packId).ToList()) await _techniqueDataService.DeleteTechniqueAsync(technique.Guid);

        var allCustomTraits = await _traitDataService.GetCustomTraitsAsync();
        foreach (var trait in allCustomTraits.Where(tr => tr.SourcePackId == packId).ToList()) await _traitDataService.DeleteTraitAsync(trait.Guid);

        var index = await LoadIndexAsync();
        var entry = index.FirstOrDefault(p => p.PackId == packId);
        if (entry != null)
        {
            string packFilePath = Path.Combine(_packsDirectory, entry.FileName);
            if (File.Exists(packFilePath)) File.Delete(packFilePath);
            index.Remove(entry);
            await SaveIndexAsync(index);
        }

        return (true, "Pack deleted.");
    }

    public async Task<(bool Success, string Message)> CopyItemsToPackAsync(
        string targetPackId,
        List<Guid> itemGuidsToCopy, List<Guid> backgroundGuidsToCopy, List<Guid> familiarGuidsToCopy,
        List<Guid> raceGuidsToCopy, List<Guid> hinderanceGuidsToCopy,
        List<Guid> blueprintGuidsToCopy, List<Guid> spellGuidsToCopy, List<Guid> quipGuidsToCopy, List<Guid> techniqueGuidsToCopy,
        List<Guid>? traitGuidsToCopy = null)
    {
        traitGuidsToCopy ??= new List<Guid>();

        var index = await LoadIndexAsync();
        var targetEntry = index.FirstOrDefault(p => p.PackId == targetPackId);
        if (targetEntry == null)
        {
            return (false, "Target pack not found.");
        }

        var allCustomItems = await _customItemStorageService.LoadCustomItemsAsync();
        var itemsToCopy = allCustomItems.Where(i => itemGuidsToCopy.Contains(i.Guid)).ToList();

        var allCustomBackgrounds = await _customBackgroundStorageService.LoadCustomBackgroundsAsync();
        var backgroundsToCopy = allCustomBackgrounds.Where(b => backgroundGuidsToCopy.Contains(b.Guid)).ToList();

        var allCustomFamiliars = await _customFamiliarStorageService.LoadCustomFamiliarsAsync();
        var familiarsToCopy = allCustomFamiliars.Where(f => familiarGuidsToCopy.Contains(f.Guid)).ToList();

        var allCustomRaces = await _customRaceStorageService.LoadCustomRacesAsync();
        var racesToCopy = allCustomRaces.Where(r => raceGuidsToCopy.Contains(r.Guid)).ToList();

        var allCustomHinderances = await _customHinderanceStorageService.LoadCustomHinderancesAsync();
        var hinderancesToCopy = allCustomHinderances.Where(h => hinderanceGuidsToCopy.Contains(h.Guid)).ToList();

        var allCustomBlueprints = await _customBlueprintStorageService.LoadCustomBlueprintsAsync();
        var blueprintsToCopy = allCustomBlueprints.Where(bp => blueprintGuidsToCopy.Contains(bp.Guid)).ToList();

        var allCustomSpells = await _customSpellStorageService.LoadCustomSpellsAsync();
        var spellsToCopy = allCustomSpells.Where(s => spellGuidsToCopy.Contains(s.Guid)).ToList();

        var allCustomQuips = await _customQuipStorageService.LoadCustomQuipsAsync();
        var quipsToCopy = allCustomQuips.Where(q => quipGuidsToCopy.Contains(q.Guid)).ToList();

        var allCustomTechniques = await _customTechniqueStorageService.LoadCustomTechniquesAsync();
        var techniquesToCopy = allCustomTechniques.Where(t => techniqueGuidsToCopy.Contains(t.Guid)).ToList();

        var allCustomTraits = await _customTraitStorageService.LoadCustomTraitsAsync();
        var traitsToCopy = allCustomTraits.Where(tr => traitGuidsToCopy.Contains(tr.Guid)).ToList();

        if (itemsToCopy.Count == 0 && backgroundsToCopy.Count == 0 && familiarsToCopy.Count == 0 && racesToCopy.Count == 0 &&
            hinderancesToCopy.Count == 0 && blueprintsToCopy.Count == 0 && spellsToCopy.Count == 0 && quipsToCopy.Count == 0 &&
            techniquesToCopy.Count == 0 && traitsToCopy.Count == 0)
        {
            return (false, "No matching assets found to copy.");
        }

        List<Item> targetPackItems = new();
        List<CharacterBackground> targetPackBackgrounds = new();
        List<Familiar> targetPackFamiliars = new();
        List<Race> targetPackRaces = new();
        List<Hinderance> targetPackHinderances = new();
        List<Blueprint> targetPackBlueprints = new();
        List<Spell> targetPackSpells = new();
        List<Quip> targetPackQuips = new();
        List<Technique> targetPackTechniques = new();
        List<CustomTrait> targetPackTraits = new();
        string? targetFilePath = null;

        if (!string.IsNullOrEmpty(targetEntry.FileName))
        {
            targetFilePath = Path.Combine(_packsDirectory, targetEntry.FileName);
            if (File.Exists(targetFilePath))
            {
                string existingJson = await File.ReadAllTextAsync(targetFilePath);
                var existingPack = JsonSerializer.Deserialize<AssetPack>(existingJson, _jsonOptions);
                targetPackItems = existingPack?.Items ?? new List<Item>();
                targetPackBackgrounds = existingPack?.Backgrounds ?? new List<CharacterBackground>();
                targetPackFamiliars = existingPack?.Familiars ?? new List<Familiar>();
                targetPackRaces = existingPack?.Races ?? new List<Race>();
                targetPackHinderances = existingPack?.Hinderances ?? new List<Hinderance>();
                targetPackBlueprints = existingPack?.Blueprints ?? new List<Blueprint>();
                targetPackSpells = existingPack?.Spells ?? new List<Spell>();
                targetPackQuips = existingPack?.Quips ?? new List<Quip>();
                targetPackTechniques = existingPack?.Techniques ?? new List<Technique>();
                targetPackTraits = existingPack?.Traits ?? new List<CustomTrait>();
            }
        }

        int itemsCopied = 0;
        foreach (var original in itemsToCopy)
        {
            var duplicate = new Item
            {
                Guid = Guid.NewGuid(),
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                Name = original.Name,
                Category = original.Category,
                BaseDescription = original.BaseDescription,
                Size = original.Size,
                ValueInChips = original.ValueInChips,
                Rarity = original.Rarity,
                Uses = original.Uses,
                QtyLimit = original.QtyLimit,
                IsStackable = original.IsStackable,
                WeaponCategory = original.WeaponCategory,
                WeaponDamage = original.WeaponDamage,
                WeaponEffects = original.WeaponEffects != null ? new List<WeaponEffectType>(original.WeaponEffects) : null,
                WeaponSpeed = original.WeaponSpeed,
                ArmorType = original.ArmorType,
                ArmorValue = original.ArmorValue
            };
            allCustomItems.Add(duplicate);
            if (targetPackId != "Local") targetPackItems.Add(duplicate);
            itemsCopied++;
        }

        int backgroundsCopied = 0;
        foreach (var original in backgroundsToCopy)
        {
            var duplicate = original.Clone();
            duplicate.Guid = Guid.NewGuid(); duplicate.Id = 0; duplicate.SourcePackId = targetPackId; duplicate.IsLoaded = true; duplicate.LastModified = DateTime.UtcNow;
            allCustomBackgrounds.Add(duplicate);
            if (targetPackId != "Local") targetPackBackgrounds.Add(duplicate);
            backgroundsCopied++;
        }

        int familiarsCopied = 0;
        foreach (var original in familiarsToCopy)
        {
            var duplicate = new Familiar
            {
                Guid = Guid.NewGuid(),
                Id = 0,
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                IsFoundation = false,
                LastModified = DateTime.UtcNow,
                FmlrName = original.FmlrName,
                FmlrDescription = original.FmlrDescription,
                FmlrClass = original.FmlrClass,
                FmlrSize = original.FmlrSize,
                FmlrIntelligence = original.FmlrIntelligence,
                HP = original.HP,
                StatVigor = original.StatVigor,
                StatAgility = original.StatAgility,
                StatMind = original.StatMind,
                StatSpirit = original.StatSpirit,
                FmlrWeaponName = original.FmlrWeaponName,
                FmlrWeaponType = original.FmlrWeaponType,
                FmlrWeaponSpeed = original.FmlrWeaponSpeed,
                FmlrWeaponDamageDie = original.FmlrWeaponDamageDie,
                Abilities = original.Abilities != null ? new List<string>(original.Abilities) : null,
                BaseInventoryCapacity = original.BaseInventoryCapacity
            };
            allCustomFamiliars.Add(duplicate);
            if (targetPackId != "Local") targetPackFamiliars.Add(duplicate);
            familiarsCopied++;
        }

        int racesCopied = 0;
        foreach (var original in racesToCopy)
        {
            var duplicate = original.Clone();
            duplicate.Guid = Guid.NewGuid(); duplicate.Id = 0; duplicate.SourcePackId = targetPackId; duplicate.IsLoaded = true; duplicate.LastModified = DateTime.UtcNow;
            allCustomRaces.Add(duplicate);
            if (targetPackId != "Local") targetPackRaces.Add(duplicate);
            racesCopied++;
        }

        int hinderancesCopied = 0;
        foreach (var original in hinderancesToCopy)
        {
            var duplicate = new Hinderance
            {
                Guid = Guid.NewGuid(),
                Id = 0,
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                Name = original.Name,
                Description = original.Description,
                VigorModifier = original.VigorModifier,
                AgilityModifier = original.AgilityModifier,
                MindModifier = original.MindModifier,
                SpiritModifier = original.SpiritModifier,
                SkillModifiers = original.SkillModifiers.Select(s => new HinderanceSkillModifier { SkillName = s.SkillName, Modifier = s.Modifier }).ToList(),
                AllowsStatBonus = original.AllowsStatBonus,
                AllowsTrainingPoint = original.AllowsTrainingPoint
            };
            allCustomHinderances.Add(duplicate);
            if (targetPackId != "Local") targetPackHinderances.Add(duplicate);
            hinderancesCopied++;
        }

        int blueprintsCopied = 0;
        foreach (var original in blueprintsToCopy)
        {
            var duplicate = new Blueprint
            {
                Guid = Guid.NewGuid(),
                Id = 0,
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                BlueprintName = original.BlueprintName,
                BlueprintDescription = original.BlueprintDescription,
                BlueprintCost = original.BlueprintCost,
                UnlockClassId = null,
                UnlockFeatureId = null
            };
            allCustomBlueprints.Add(duplicate);
            if (targetPackId != "Local") targetPackBlueprints.Add(duplicate);
            blueprintsCopied++;
        }

        int spellsCopied = 0;
        foreach (var original in spellsToCopy)
        {
            var duplicate = new Spell
            {
                Guid = Guid.NewGuid(),
                Id = 0,
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                SpellName = original.SpellName,
                SpellDescription = original.SpellDescription,
                SpellDice = original.SpellDice,
                SpellRange = original.SpellRange,
                UnlockClassId = null,
                UnlockFeatureId = null
            };
            allCustomSpells.Add(duplicate);
            if (targetPackId != "Local") targetPackSpells.Add(duplicate);
            spellsCopied++;
        }

        int quipsCopied = 0;
        foreach (var original in quipsToCopy)
        {
            var duplicate = new Quip
            {
                Guid = Guid.NewGuid(),
                Id = 0,
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                QuipName = original.QuipName,
                QuipDescription = original.QuipDescription,
                UnlockClassId = null,
                UnlockFeatureId = null
            };
            allCustomQuips.Add(duplicate);
            if (targetPackId != "Local") targetPackQuips.Add(duplicate);
            quipsCopied++;
        }

        int techniquesCopied = 0;
        foreach (var original in techniquesToCopy)
        {
            var duplicate = new Technique
            {
                Guid = Guid.NewGuid(),
                Id = 0,
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                TechniqueName = original.TechniqueName,
                TechniqueDescription = original.TechniqueDescription,
                UnlockClassId = null,
                UnlockFeatureId = 0
            };
            allCustomTechniques.Add(duplicate);
            if (targetPackId != "Local") targetPackTechniques.Add(duplicate);
            techniquesCopied++;
        }

        int traitsCopied = 0;
        foreach (var original in traitsToCopy)
        {
            var duplicate = new CustomTrait
            {
                Guid = Guid.NewGuid(),
                SourcePackId = targetPackId,
                IsLoaded = true,
                IsPlayerCreated = true,
                LastModified = DateTime.UtcNow,
                TraitName = original.TraitName,
                PD = original.PD,
                Effect = original.Effect,
                Vigor = original.Vigor,
                Agility = original.Agility,
                Armor = original.Armor,
                WeaponDie = original.WeaponDie,
                WeaponType = original.WeaponType,
                Blast = original.Blast,
                Element = original.Element,
                ChooseElement = original.ChooseElement,
                GrantsPack = original.GrantsPack,
                IsSmall = original.IsSmall,
                IsLarge = original.IsLarge,
                RequiresSmall = original.RequiresSmall,
                ForbidsLarge = original.ForbidsLarge
            };
            allCustomTraits.Add(duplicate);
            if (targetPackId != "Local") targetPackTraits.Add(duplicate);
            traitsCopied++;
        }

        await _customItemStorageService.SaveCustomItemsAsync(allCustomItems);
        await _customBackgroundStorageService.SaveCustomBackgroundsAsync(allCustomBackgrounds);
        await _customFamiliarStorageService.SaveCustomFamiliarsAsync(allCustomFamiliars);
        await _customRaceStorageService.SaveCustomRacesAsync(allCustomRaces);
        await _customHinderanceStorageService.SaveCustomHinderancesAsync(allCustomHinderances);
        await _customBlueprintStorageService.SaveCustomBlueprintsAsync(allCustomBlueprints);
        await _customSpellStorageService.SaveCustomSpellsAsync(allCustomSpells);
        await _customQuipStorageService.SaveCustomQuipsAsync(allCustomQuips);
        await _customTechniqueStorageService.SaveCustomTechniquesAsync(allCustomTechniques);
        await _customTraitStorageService.SaveCustomTraitsAsync(allCustomTraits);

        if (targetFilePath != null)
        {
            var updatedPack = new AssetPack
            {
                PackId = targetPackId,
                Name = targetEntry.Name,
                Items = targetPackItems,
                Backgrounds = targetPackBackgrounds,
                Familiars = targetPackFamiliars,
                Races = targetPackRaces,
                Hinderances = targetPackHinderances,
                Blueprints = targetPackBlueprints,
                Spells = targetPackSpells,
                Quips = targetPackQuips,
                Techniques = targetPackTechniques,
                Traits = targetPackTraits
            };
            string json = JsonSerializer.Serialize(updatedPack, _jsonOptions);
            await File.WriteAllTextAsync(targetFilePath, json);
        }

        return (true, $"Copied {itemsCopied} item(s), {backgroundsCopied} background(s), {familiarsCopied} familiar(s), {racesCopied} race(s), {hinderancesCopied} hinderance(s), {blueprintsCopied} blueprint(s), {spellsCopied} spell(s), {quipsCopied} quip(s), {techniquesCopied} technique(s), and {traitsCopied} trait(s) to '{targetEntry.Name}'.");
    }

    private async Task<List<InstalledPackEntry>> LoadIndexAsync()
    {
        if (!File.Exists(_indexFilePath)) return new List<InstalledPackEntry>();
        try
        {
            string json = await File.ReadAllTextAsync(_indexFilePath);
            return JsonSerializer.Deserialize<List<InstalledPackEntry>>(json, _jsonOptions) ?? new();
        }
        catch { return new List<InstalledPackEntry>(); }
    }

    private async Task SaveIndexAsync(List<InstalledPackEntry> index)
    {
        string json = JsonSerializer.Serialize(index, _jsonOptions);
        await File.WriteAllTextAsync(_indexFilePath, json);
    }

    public async Task<(bool Success, string Message)> UpdatePackAsync(string packId, string name, string? description, string? details, PackAssetSyncMode syncMode)
    {
        var index = await LoadIndexAsync();
        var entry = index.FirstOrDefault(p => p.PackId == packId);
        if (entry == null)
        {
            return (false, "Pack not found.");
        }

        bool isLocal = string.IsNullOrEmpty(entry.FileName);
        if (isLocal && syncMode != PackAssetSyncMode.None)
        {
            return (false, "The Local pack can't be asset-synced this way.");
        }

        entry.Name = name;
        entry.Description = description;
        entry.Details = details;

        string message = "Pack details updated.";

        if (syncMode != PackAssetSyncMode.None)
        {
            var storedItems = await _customItemStorageService.LoadCustomItemsAsync();
            var storedBackgrounds = await _customBackgroundStorageService.LoadCustomBackgroundsAsync();
            var storedFamiliars = await _customFamiliarStorageService.LoadCustomFamiliarsAsync();
            var storedRaces = await _customRaceStorageService.LoadCustomRacesAsync();
            var storedHinderances = await _customHinderanceStorageService.LoadCustomHinderancesAsync();
            var storedBlueprints = await _customBlueprintStorageService.LoadCustomBlueprintsAsync();
            var storedSpells = await _customSpellStorageService.LoadCustomSpellsAsync();
            var storedQuips = await _customQuipStorageService.LoadCustomQuipsAsync();
            var storedTechniques = await _customTechniqueStorageService.LoadCustomTechniquesAsync();
            var storedTraits = await _customTraitStorageService.LoadCustomTraitsAsync();

            List<Item> updatedPackItems;
            List<CharacterBackground> updatedPackBackgrounds;
            List<Familiar> updatedPackFamiliars;
            List<Race> updatedPackRaces;
            List<Hinderance> updatedPackHinderances;
            List<Blueprint> updatedPackBlueprints;
            List<Spell> updatedPackSpells;
            List<Quip> updatedPackQuips;
            List<Technique> updatedPackTechniques;
            List<CustomTrait> updatedPackTraits;

            if (syncMode == PackAssetSyncMode.OverwriteOnly)
            {
                updatedPackItems = storedItems.Where(i => i.SourcePackId == packId).ToList();
                updatedPackBackgrounds = storedBackgrounds.Where(b => b.SourcePackId == packId).ToList();
                updatedPackFamiliars = storedFamiliars.Where(f => f.SourcePackId == packId).ToList();
                updatedPackRaces = storedRaces.Where(r => r.SourcePackId == packId).ToList();
                updatedPackHinderances = storedHinderances.Where(h => h.SourcePackId == packId).ToList();
                updatedPackBlueprints = storedBlueprints.Where(bp => bp.SourcePackId == packId).ToList();
                updatedPackSpells = storedSpells.Where(s => s.SourcePackId == packId).ToList();
                updatedPackQuips = storedQuips.Where(q => q.SourcePackId == packId).ToList();
                updatedPackTechniques = storedTechniques.Where(t => t.SourcePackId == packId).ToList();
                updatedPackTraits = storedTraits.Where(tr => tr.SourcePackId == packId).ToList();

                message = $"'{name}' updated with the current state of its {updatedPackItems.Count} item(s), {updatedPackBackgrounds.Count} background(s), {updatedPackFamiliars.Count} familiar(s), {updatedPackRaces.Count} race(s), {updatedPackHinderances.Count} hinderance(s), {updatedPackBlueprints.Count} blueprint(s), {updatedPackSpells.Count} spell(s), {updatedPackQuips.Count} quip(s), {updatedPackTechniques.Count} technique(s), and {updatedPackTraits.Count} trait(s).";
            }
            else
            {
                var loadedItems = storedItems.Where(i => i.IsLoaded).ToList();
                var loadedBackgrounds = storedBackgrounds.Where(b => b.IsLoaded).ToList();
                var loadedFamiliars = storedFamiliars.Where(f => f.IsLoaded).ToList();
                var loadedRaces = storedRaces.Where(r => r.IsLoaded).ToList();
                var loadedHinderances = storedHinderances.Where(h => h.IsLoaded).ToList();
                var loadedBlueprints = storedBlueprints.Where(bp => bp.IsLoaded).ToList();
                var loadedSpells = storedSpells.Where(s => s.IsLoaded).ToList();
                var loadedQuips = storedQuips.Where(q => q.IsLoaded).ToList();
                var loadedTechniques = storedTechniques.Where(t => t.IsLoaded).ToList();
                var loadedTraits = storedTraits.Where(tr => tr.IsLoaded).ToList();

                foreach (var item in loadedItems) if (item.SourcePackId != packId) item.SourcePackId = packId;
                foreach (var background in loadedBackgrounds) if (background.SourcePackId != packId) background.SourcePackId = packId;
                foreach (var familiar in loadedFamiliars) if (familiar.SourcePackId != packId) familiar.SourcePackId = packId;
                foreach (var race in loadedRaces) if (race.SourcePackId != packId) race.SourcePackId = packId;
                foreach (var hinderance in loadedHinderances) if (hinderance.SourcePackId != packId) hinderance.SourcePackId = packId;
                foreach (var blueprint in loadedBlueprints) if (blueprint.SourcePackId != packId) blueprint.SourcePackId = packId;
                foreach (var spell in loadedSpells) if (spell.SourcePackId != packId) spell.SourcePackId = packId;
                foreach (var quip in loadedQuips) if (quip.SourcePackId != packId) quip.SourcePackId = packId;
                foreach (var technique in loadedTechniques) if (technique.SourcePackId != packId) technique.SourcePackId = packId;
                foreach (var trait in loadedTraits) if (trait.SourcePackId != packId) trait.SourcePackId = packId;

                updatedPackItems = loadedItems;
                updatedPackBackgrounds = loadedBackgrounds;
                updatedPackFamiliars = loadedFamiliars;
                updatedPackRaces = loadedRaces;
                updatedPackHinderances = loadedHinderances;
                updatedPackBlueprints = loadedBlueprints;
                updatedPackSpells = loadedSpells;
                updatedPackQuips = loadedQuips;
                updatedPackTechniques = loadedTechniques;
                updatedPackTraits = loadedTraits;

                message = $"'{name}' updated: {updatedPackItems.Count} item(s), {updatedPackBackgrounds.Count} background(s), {updatedPackFamiliars.Count} familiar(s), {updatedPackRaces.Count} race(s), {updatedPackHinderances.Count} hinderance(s), {updatedPackBlueprints.Count} blueprint(s), {updatedPackSpells.Count} spell(s), {updatedPackQuips.Count} quip(s), {updatedPackTechniques.Count} technique(s), and {updatedPackTraits.Count} trait(s) now loaded into the pack.";

                foreach (var item in loadedItems)
                {
                    var live = await _itemDataService.GetItemByGuidAsync(item.Guid);
                    if (live != null) live.SourcePackId = item.SourcePackId;
                }
                foreach (var background in loadedBackgrounds)
                {
                    var live = await _backgroundDataService.GetBackgroundByGuidAsync(background.Guid);
                    if (live != null) live.SourcePackId = background.SourcePackId;
                }
                foreach (var familiar in loadedFamiliars)
                {
                    var live = await _familiarDataService.GetFamiliarByGuidAsync(familiar.Guid);
                    if (live != null) live.SourcePackId = familiar.SourcePackId;
                }
                foreach (var race in loadedRaces)
                {
                    var live = await _raceDataService.GetRaceByGuidAsync(race.Guid);
                    if (live != null) live.SourcePackId = race.SourcePackId;
                }
                foreach (var hinderance in loadedHinderances)
                {
                    var live = await _hinderanceDataService.GetHinderanceByGuidAsync(hinderance.Guid);
                    if (live != null) live.SourcePackId = hinderance.SourcePackId;
                }
                foreach (var blueprint in loadedBlueprints)
                {
                    var live = await _blueprintDataService.GetBlueprintByGuidAsync(blueprint.Guid);
                    if (live != null) live.SourcePackId = blueprint.SourcePackId;
                }
                foreach (var spell in loadedSpells)
                {
                    var live = await _spellDataService.GetSpellByGuidAsync(spell.Guid);
                    if (live != null) live.SourcePackId = spell.SourcePackId;
                }
                foreach (var quip in loadedQuips)
                {
                    var live = await _quipDataService.GetQuipByGuidAsync(quip.Guid);
                    if (live != null) live.SourcePackId = quip.SourcePackId;
                }
                foreach (var technique in loadedTechniques)
                {
                    var live = await _techniqueDataService.GetTechniqueByGuidAsync(technique.Guid);
                    if (live != null) live.SourcePackId = technique.SourcePackId;
                }
                foreach (var trait in loadedTraits)
                {
                    var live = await _traitDataService.GetTraitByGuidAsync(trait.Guid);
                    if (live != null) live.SourcePackId = trait.SourcePackId;
                }
            }

            await _customItemStorageService.SaveCustomItemsAsync(storedItems);
            await _customBackgroundStorageService.SaveCustomBackgroundsAsync(storedBackgrounds);
            await _customFamiliarStorageService.SaveCustomFamiliarsAsync(storedFamiliars);
            await _customRaceStorageService.SaveCustomRacesAsync(storedRaces);
            await _customHinderanceStorageService.SaveCustomHinderancesAsync(storedHinderances);
            await _customBlueprintStorageService.SaveCustomBlueprintsAsync(storedBlueprints);
            await _customSpellStorageService.SaveCustomSpellsAsync(storedSpells);
            await _customQuipStorageService.SaveCustomQuipsAsync(storedQuips);
            await _customTechniqueStorageService.SaveCustomTechniquesAsync(storedTechniques);
            await _customTraitStorageService.SaveCustomTraitsAsync(storedTraits);

            var updatedPack = new AssetPack
            {
                PackId = packId,
                Name = name,
                Items = updatedPackItems,
                Backgrounds = updatedPackBackgrounds,
                Familiars = updatedPackFamiliars,
                Races = updatedPackRaces,
                Hinderances = updatedPackHinderances,
                Blueprints = updatedPackBlueprints,
                Spells = updatedPackSpells,
                Quips = updatedPackQuips,
                Techniques = updatedPackTechniques,
                Traits = updatedPackTraits
            };
            string packFilePath = Path.Combine(_packsDirectory, entry.FileName);
            string json = JsonSerializer.Serialize(updatedPack, _jsonOptions);
            await File.WriteAllTextAsync(packFilePath, json);
        }

        await SaveIndexAsync(index);

        return (true, message);
    }
}