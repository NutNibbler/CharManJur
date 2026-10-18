namespace CharManJur.Models;

// What happened when a day ended, so the UI can tell the player.
public class DayEndResult
{
    public bool MissedNeeds { get; set; }
    public bool MissedRest { get; set; }
    public bool BecameDeprived { get; set; }   // first deprived day
    public bool StillDeprived { get; set; }
    public int FatigueGained { get; set; }
    public int DeprivedDays { get; set; }
}