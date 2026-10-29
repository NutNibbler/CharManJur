using System.Text.Json.Serialization;
using CharManJur.Services;   // DamageTargetType lives in Models or Services; this using is harmless either way

namespace CharManJur.Models;

// How fast the attack is. It matters for Block and Dodge, which Impair different speeds.
public enum IncomingAttackSpeed { Slow, Balanced, Fast }

// Damage the GM has sent to a player. It waits on the character until the player takes it on or ignores it.
public class IncomingDamage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SentUtc { get; set; } = DateTime.UtcNow;
    public DamageTargetType Target { get; set; } = DamageTargetType.Hitpoints;
    public int Amount { get; set; }
    public IncomingAttackSpeed Speed { get; set; } = IncomingAttackSpeed.Balanced;   // only matters for Hitpoints damage
    public string? Note { get; set; }

    [JsonIgnore]
    public string Summary => Target == DamageTargetType.Hitpoints
        ? $"{Amount} Hitpoints damage · {Speed} attack"
        : $"{Amount} {Target} damage";
}

// An effect the GM has sent to a player. The player takes it on (or ignores it) from their Apply Effect button.
public class IncomingEffect
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SentUtc { get; set; } = DateTime.UtcNow;
    public CharacterEffect Effect { get; set; } = new();

}