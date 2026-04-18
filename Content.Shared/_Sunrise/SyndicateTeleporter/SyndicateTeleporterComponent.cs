using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
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
}
