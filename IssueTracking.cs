using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur
{
    internal class IssueTracking
    {
        /*
        Multi-Limb System Needed:
            *Focused only on preshensile limbs for now.
            Race, Feature, of Mid-Game developements may affect limb counts
            *Prehensile Limbs
                *Pair
                *Single
                    *Single set limbs can't equip two-handeds
            *Two-handed items must be equipped by a complete limb pair, not one of two diffent pairs, or a single + one limb of a pair. Visual aid in interface would be beneficial.

        
        Class and Feature Selection:
            Race Features(Fiend) need to add languages.
            Selected sub-feature(s) is remembered, but proceed button asks for a sub-feature selection anyways if you're returning to the page, from loading or going back.

        Background Selection:
            Ability score(stat) bonus doesn't show "No Stat bonus" when there isn't one for a class, instead its blank.
            Familiar system is not working, familiars don't populate from dataservice.
            Confirm background button converters don't exist, so button is always grey and says True/False. Backgrounds should not be confirmable until their available choices are picked including familiars.
            Ability Score(stat) and Skill bonuses aren't being saved and added to background bonus data. Both ability scores and skill bonuses should show up as "Background Bonuses" and saved as such in the character's JSON.
        */
    }
}
