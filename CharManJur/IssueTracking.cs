using System;
using System.Collections.Generic;
using System.Text;

namespace CharManJur
{
    internal class IssueTracking
    {
        /*
        The GM dashboard will be the similar in shape as the player home page:

1. Upper panel, containing headlining information.
    1. Headlining information panel:
        1. In-Game Day(Both counting from 1, and the day of the week. Calendar is configured per session by GM at session creation.
        2. Time slider, used for tracking what time it is in the current day.
        3. Name of each player and character connected "CharacterName(PlayerName)"
        4. Character's HP and Vigor Stats
        5. Where "Begin Combat","End Combat", "Pass Day" buttons are.
        6. The session is opened and closed. A closed sessions prevents people from joining, and is the default mode when a session is created/loaded.
        
2. Large, LEFT(instead of right) sided panel, which hosts the actively selected panel, similar to how the characterhomepage uses the inventory/equipment panels  for other sub-panels.
    1. Home Panel(Panel that GM starts on).
        1. Narrative tracker(Simplest-Body of text/Complex-Web layout connecting text bubbles, characters, images,etc..)
        2. Map tracker
            1. Very similar to the image pages of the scratch pad, uploaded images, able to written on top of. Except it needs shapes or icons that can be custom made and moved around the map.
    2. Bestow Panel(Intended for batch bestowments, but can be used to bestow onto a single player)
        1. Pick which players get this object.
        2. Pick the object category(Spell, Quip, Technique, Blueprint, Item, Familiar, Scratch Pad Page).
        3. Pick the object(s) being bestowed.
    3. Player Interface Panel
        1. Where invitations are drafted, whether its an invite code or something else.
        2. Session Password setup(Optional).
        3. Player list. 
            1. Where kick player exists.
            2. Apply Effect(Same effect creator that is in the player home page)
            3. Apply Damage(Same damage page that is in the player home page)
            4. Bestow Item button(Same bestow page that is in the player home page)
            5. Bestow Familiar button (Same bestow page that is in the player home page)
            6. Bestow Sub-Feature (Same page as the player home page)
            7. Bestow Scratch Pad Page.
    4. Player Viewer Panel
        1. Choose a player that is connected.
            1. You get the exact same view as the character home page, but with that player's character data. This enables the viewing of all elements of the player's character
        2. Bottom row, instead of the Options button, the "Close Viewer" button is there
        3. Sliding panel that overtakes the entire screen, isntead of just the bottom two panels.
    5. Profiles Panel
        1. This panel holds NPC profiles, and the creator.
        2. List of all created NPCs
        3. NPC Attributes: Name, Kin, Faction, Generic Description, Language(s), Location, Profession, Personality, Accent, Image, Stat block
        4. NPC attributes can be customized, removed, or added during campaign creation(Before the GM dashboard is even available)
    6. Shop Panel
        1. Shop can be opened here using a shop open wizard
            1. First Iteration:
                1. Open Shop/Choose Players that get this shop/Decide which item categories are available/Set Buy/Sell inflation rates(Slider)
            2. Second Iteration(Future plan):
                1. Open ship/Choose players that get this shop/Decide which item categories are available, Set max stocks of each rarity(40 Base,20 Common,4 Uncommon,0-1 Rare)/roll for item stock based on rarity, Set Buy/Sell inflation rates(Sldier).
        
3. Smaller, RIGHT(instead of left) sided pane, which hosts the event log.
   1. Filter options by player and/or event type, search bar that searches event log messages text.
        */
    }
}
