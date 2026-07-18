using Microsoft.Maui.Controls;
using CharManJur.Models;

namespace CharManJur.Converters;

public class SubFeatureTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BlueprintTemplate { get; set; }
    public DataTemplate? QuipTemplate { get; set; }
    public DataTemplate? SpellTemplate { get; set; }
    public DataTemplate? TechniqueTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            Blueprint _ => BlueprintTemplate ?? new DataTemplate(),
            Quip _ => QuipTemplate ?? new DataTemplate(),
            Spell _ => SpellTemplate ?? new DataTemplate(),
            Technique _ => TechniqueTemplate ?? new DataTemplate(),
            _ => new DataTemplate()
        };
    }
}