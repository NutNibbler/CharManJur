using CharManJur.Models;

public class LanguageDisplay
{
    public Language Language { get; set; } = new();
    public bool IsSelected { get; set; }

    // Helper properties for binding
    public int Id => Language.Id;
    public string Name => Language.Name;
    public string Description => Language.Description;
}