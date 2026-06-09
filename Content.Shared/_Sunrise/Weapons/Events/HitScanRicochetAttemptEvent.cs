// Sunrise-Edit

using System.Numerics;

namespace Content.Shared._Sunrise.Weapons.Events;

[ByRefEvent]
public record struct HitScanRicochetAttemptEvent(float Chance, Vector2 Pos, Vector2 Dir, bool Ricocheted, Vector2? WorldNormal = null)
{
}
