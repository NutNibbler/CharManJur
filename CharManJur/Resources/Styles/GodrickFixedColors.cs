namespace CharManJur.Resources.Styles;

public static class GodrickFixedColors
{
    public static void Register(ResourceDictionary resources)
    {
        resources["VigorColor"] = Colors.Red;
        resources["AgilityColor"] = Colors.Green;
        resources["MindColor"] = Colors.Orange;
        resources["SpiritColor"] = Colors.Aqua;
        resources["HitpointsColor"] = Colors.Silver;

        resources["SpellColor"] = Colors.SkyBlue;
        resources["BlueprintColor"] = Colors.LimeGreen;
        resources["QuipColor"] = Colors.Purple;
        resources["TechniqueColor"] = Colors.Orange;
    }
}