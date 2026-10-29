using CharManJur.Services;
using CharManJur.ViewModels;
using CharManJur.ViewModels.Godrick_LiveGame;
using CharManJur.Views;
using CharManJur.Views.Godrick_LiveGame;
using Microsoft.Extensions.Logging;

namespace CharManJur
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            CrashLog.Hook();
            CharManJur.Helpers.ThemeContrast.Register();
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<ICharAttribDataService, CharAttribDataService>();
            builder.Services.AddSingleton<IGlobalMenuDataService, GlobalMenuDataService>();
            builder.Services.AddSingleton<IRaceDataService, RaceDataService>();
            builder.Services.AddTransient<RaceSelectionViewModel>();
            builder.Services.AddSingleton<IClassDataService, ClassDataService>();
            builder.Services.AddTransient<ClassSelectionViewModel>();
            builder.Services.AddSingleton<IQuipDataService, QuipDataService>();
            builder.Services.AddSingleton<ISpellDataService, SpellDataService>();
            builder.Services.AddSingleton<ITechniqueDataService, TechniqueDataService>();
            builder.Services.AddSingleton<IBlueprintDataService, BlueprintDataService>();
            builder.Services.AddTransient<SubFeatureSelectionViewModel>();
            builder.Services.AddSingleton<ICharacterPersistenceService, CharacterPersistenceService>();
            builder.Services.AddTransient<CharBuilder_Godrick_SubFeatureSelectionPopup>();
            builder.Services.AddTransient<LoadCharacterViewModel>();
            builder.Services.AddSingleton<IBackgroundDataService, BackgroundDataService>();
            builder.Services.AddSingleton<IItemDataService, ItemDataService>();
            builder.Services.AddSingleton<IItemTransferGateway, LocalOnlyItemTransferGateway>();

            //APPLICATION SETTINGS REGISTERS
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<ISkinService, SkinService>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();

            //ASSET MANAGER REGISTERS
            builder.Services.AddTransient<ItemAssetManagerViewModel>();
            builder.Services.AddTransient<ItemAssetManagerPage>();
            builder.Services.AddTransient<BackgroundAssetManagerViewModel>();
            builder.Services.AddTransient<BackgroundAssetManagerPage>();
            builder.Services.AddSingleton<IAssetPackService, AssetPackService>();
            builder.Services.AddTransient<FamiliarAssetManagerViewModel>();
            builder.Services.AddTransient<FamiliarAssetManagerPage>();
            builder.Services.AddTransient<RaceAssetManagerViewModel>();
            builder.Services.AddTransient<RaceAssetManagerPage>();
            builder.Services.AddTransient<HinderanceAssetManagerViewModel>();
            builder.Services.AddTransient<HinderanceAssetManagerPage>();
            builder.Services.AddTransient<BlueprintAssetManagerViewModel>();
            builder.Services.AddTransient<BlueprintAssetManagerPage>();
            builder.Services.AddTransient<SpellAssetManagerViewModel>();
            builder.Services.AddTransient<SpellAssetManagerPage>();
            builder.Services.AddTransient<QuipAssetManagerViewModel>();
            builder.Services.AddTransient<QuipAssetManagerPage>();
            builder.Services.AddTransient<TechniqueAssetManagerViewModel>();
            builder.Services.AddTransient<TechniqueAssetManagerPage>();

            //CUSTOM HINDERANCE REGISTERS
            builder.Services.AddSingleton<ICustomHinderanceStorageService, CustomHinderanceStorageService>();
            builder.Services.AddTransient<Godrick_CustomHinderanceCreator>();

            //CUSTOM FAMILIAR REGISTERS
            builder.Services.AddSingleton<IFamiliarDataService, FamiliarDataService>();
            builder.Services.AddSingleton<ICustomFamiliarStorageService, CustomFamiliarStorageService>();

            //CUSTOM SUB FEATURE REGISTERS
            builder.Services.AddSingleton<ICustomBlueprintStorageService, CustomBlueprintStorageService>();
            builder.Services.AddSingleton<ICustomSpellStorageService, CustomSpellStorageService>();
            builder.Services.AddSingleton<ICustomQuipStorageService, CustomQuipStorageService>();
            builder.Services.AddSingleton<ICustomTechniqueStorageService, CustomTechniqueStorageService>();

            builder.Services.AddTransient<BackgroundSelectionViewModel>();
            builder.Services.AddSingleton<ICustomItemStorageService, CustomItemStorageService>();
            builder.Services.AddSingleton<CharacterHomeViewModel>();
            builder.Services.AddSingleton<IHinderanceDataService, HinderanceDataService>();
            builder.Services.AddTransient<HinderanceSelectionViewModel>();
            builder.Services.AddSingleton<ILanguageDataService, LanguageDataService>();
            builder.Services.AddTransient<TrainingPopupViewModel>();
            builder.Services.AddTransient<Godrick_Training_Popup>();
            builder.Services.AddTransient<EquippedItemContextMenu>();
            builder.Services.AddSingleton<IItemRecoveryService, ItemRecoveryService>();
            builder.Services.AddSingleton<IPlayerActionLogService, PlayerActionLogService>();
            //MULTIPLAYER AND CAMPAIGN MANAGER REGISTERS
            builder.Services.AddSingleton<ICampaignStore, CampaignStore>();
            builder.Services.AddTransient<CampaignCreatorViewModel>();
            builder.Services.AddTransient<CampaignCreatorPage>();
            builder.Services.AddTransient<CampaignListViewModel>();
            builder.Services.AddTransient<CampaignListPage>();
            builder.Services.AddTransient<GmDashboardViewModel>();
            builder.Services.AddTransient<GmDashboardPage>();
            builder.Services.AddSingleton<IGmEventLog, GmEventLog>();
            builder.Services.AddSingleton<IGmSessionHub, GmSessionHub>();
            builder.Services.AddTransient<GmEventLogViewModel>();
            builder.Services.AddSingleton<IGmBestowService, GmBestowService>();
            builder.Services.AddTransient<GmBestowViewModel>();
            builder.Services.AddSingleton<IGmCharacterService, GmCharacterService>();
            builder.Services.AddSingleton<IRemoteBestowService, RemoteBestowService>();
            builder.Services.AddTransient<GmPlayersViewModel>();
            builder.Services.AddTransient<GmViewerViewModel>();
            builder.Services.AddTransient<GmMapViewModel>();
            //PLAYER SIDE OF A GM SESSION
            builder.Services.AddSingleton<IPlayerSession, PlayerSession>();
            builder.Services.AddTransient<JoinSessionViewModel>();
            builder.Services.AddTransient<JoinSessionPage>();

            //CUSTOM RACE BUILDER REGISTER
            builder.Services.AddTransient<Godrick_CustomRaceCreator>();
            builder.Services.AddSingleton<ICustomRaceStorageService, CustomRaceStorageService>();
            builder.Services.AddTransient<CustomRaceBuilderViewModel>();

            //CUSTOM TRAIT REGISTERS (Shifter traits)
            builder.Services.AddSingleton<ICustomTraitStorageService, CustomTraitStorageService>();
            builder.Services.AddSingleton<ITraitDataService, TraitDataService>();
            builder.Services.AddTransient<TraitAssetManagerViewModel>();
            builder.Services.AddTransient<TraitAssetManagerPage>();

            //CUSTOM BACKGROUND BUILDER REGISTER
            builder.Services.AddSingleton<ICustomBackgroundStorageService, CustomBackgroundStorageService>();
            builder.Services.AddTransient<CustomBackgroundBuilderViewModel>();
            builder.Services.AddTransient<Godrick_CustomBackgroundCreator>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}