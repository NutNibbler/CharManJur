namespace CharManJur.Models;

// Image keys a skin pack can provide. Matched against file names (without extension), case-insensitive.
public static class SkinKeys
{
    public const string Primary = "primary";
    public const string Neutral = "neutral";
    public const string Danger = "danger";
    public const string Warning = "warning";
    public const string Create = "create";
    public const string Confirm = "confirm";
    public const string Summary = "summary";
    public const string Generic = "generic";
    public const string GenericSmall = "generic_small";
    public const string ConfirmPressed = "confirm_pressed";
    public const string ConfirmDisabled = "confirm_disabled";
    public const string Panel = "panel";
    public const string PageBackground = "page_background";
    public const string ItemRarity0 = "item_rarity0";
    public const string ItemRarity1 = "item_rarity1";
    public const string ItemRarity2 = "item_rarity2";
    public const string ItemRarity3 = "item_rarity3";
}

// Nine-slice corner sizes in pixels of the source image.
public readonly record struct NineSliceMargins(double Left, double Top, double Right, double Bottom)
{
    public static NineSliceMargins Uniform(double value) => new(value, value, value, value);
}

public class SkinFontInfo
{
    public string PackFolder { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Saved in Preferences to remember the choice: "PackFolder|FileName"
    public string Key => $"{PackFolder}|{FileName}";
}

// One skin pack found on disk (a folder under Data\Skins).
public class SkinPack
{
    public const double DefaultNineSlice = 16;

    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;

    // From skin.json, or the folder name when there is none
    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Set when skin.json exists but couldn't be read
    public string? Warning { get; set; }

    // image key (file name without extension) -> full path
    public Dictionary<string, string> Images { get; } = new(StringComparer.OrdinalIgnoreCase);

    public NineSliceMargins DefaultMargins { get; set; } = NineSliceMargins.Uniform(DefaultNineSlice);

    // per-image overrides from skin.json
    public Dictionary<string, NineSliceMargins> ImageMargins { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<SkinFontInfo> Fonts { get; } = new();
}

// One line of the saved load order.
public class SkinPackSetting
{
    public string Folder { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

// The result of looking an image key up through the load order.
public class ResolvedSkinImage
{
    public string Key { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string PackFolder { get; set; } = string.Empty;
    public string PackName { get; set; } = string.Empty;
    public NineSliceMargins Margins { get; set; }
}