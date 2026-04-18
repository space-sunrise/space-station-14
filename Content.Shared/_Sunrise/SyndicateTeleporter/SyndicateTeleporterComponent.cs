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
    /// Duration of the knockdown applied to the user after teleportation landing.
    /// Set to null to disable the knockdown effect.
    /// </summary>
    [DataField]
    public TimeSpan? KnockdownDuration;
}
