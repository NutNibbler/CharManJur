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

            //APPLICATION SETTINGS REGISTERS
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();

            //CUSTOM HINDERANCE REGISTERS
            builder.Services.AddSingleton<ICustomHinderanceStorageService, CustomHinderanceStorageService>();
            builder.Services.AddTransient<Godrick_CustomHinderanceCreator>();

            //CUSTOM FAMILIAR REGISTERS
            builder.Services.AddSingleton<IFamiliarDataService, FamiliarDataService>();
            builder.Services.AddSingleton<ICustomFamiliarStorageService, CustomFamiliarStorageService>();

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

            //CUSTOM RACE BUILDER REGISTER
            builder.Services.AddTransient<Godrick_CustomRaceCreator>();
            builder.Services.AddSingleton<ICustomRaceStorageService, CustomRaceStorageService>();
            builder.Services.AddTransient<CustomRaceBuilderViewModel>();

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
