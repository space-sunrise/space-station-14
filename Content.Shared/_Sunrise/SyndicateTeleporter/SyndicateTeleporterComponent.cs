using Robust.Shared.GameObjects;
using Content.Shared.Damage;

namespace Content.Shared._Sunrise.SyndicateTeleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    [DataField]
 	public int RandomDistanceValue = 4;

    [DataField]
	public float TeleportationValue = 4f;

    [DataField]
 	public DamageSpecifier? DamageOnBlocked;

    /// <summary>
    /// Duration of the knockdown applied to entities at the teleport destination.
    /// Set to null to disable the knockdown effect.
    /// </summary>
    [DataField]
    public TimeSpan? KnockdownDuration;

    /// <summary>
    /// Random offset in tiles applied to the user's landing position.
    /// The user lands within this radius of the target point.
    /// Set to 0 to land exactly at the target.
    /// </summary>
    [DataField]
    public float LandingRandomOffset = 1f;
}
