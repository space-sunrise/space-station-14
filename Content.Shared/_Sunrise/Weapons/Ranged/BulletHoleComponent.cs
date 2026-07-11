using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Weapons.Ranged;

/// <summary>
/// Stores the server-side state of bullet holes on a surface.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BulletHoleSystem))]
public sealed partial class BulletHoleComponent : Component
{
    /// <summary>
    /// Number of confirmed hits.
    /// </summary>
    public int Count;

    /// <summary>
    /// Selected bullet-hole layout.
    /// </summary>
    public int State;
}

/// <summary>
/// Marks a projectile that can leave bullet holes after damaging a wall.
/// </summary>
[RegisterComponent]
public sealed partial class BulletHoleGeneratorComponent : Component
{
    /// <summary>
    /// Damage type that must be dealt for a bullet hole to be created.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> RequiredDamageType = "Piercing";
}

[Serializable, NetSerializable]
public enum BulletHoleVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum BulletHoleVisualLayers : byte
{
    BulletHole,
}
