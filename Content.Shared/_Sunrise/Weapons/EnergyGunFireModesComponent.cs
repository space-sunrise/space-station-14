using Content.Shared.Weapons.Ranged;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Weapons;

/// <summary>
/// Allows for energy gun to switch between three modes. This also changes the sprite accordingly.
/// </summary>
/// <remarks>This is BatteryWeaponFireModesSystem with additional changes to allow for different sprites.</remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EnergyGunFireModesSystem))]
[AutoGenerateComponentState]
public sealed partial class EnergyGunFireModesComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the energy gun can switch between
    /// </summary>
    [DataField("fireModes", required: true)]
    [AutoNetworkedField]
    public List<EnergyGunFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField("currentFireMode")]
    [AutoNetworkedField]
    public EnergyGunFireMode? CurrentFireMode = default!;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class EnergyGunFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("projectileProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ProjectilePrototype;

    /// <summary>
    /// The hitscan prototype associated with this firing mode
    /// </summary>
    [DataField("hitscanProto", customTypeSerializer: typeof(PrototypeIdSerializer<HitscanPrototype>))]
    public string? HitscanPrototype;

    [DataField]
    public float FireCost = 100;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public ShotType ShotType = ShotType.Projectile;

    /// <summary>
    /// The name of the selected firemode
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// What RsiState we use for that firemode if it needs to change.
    /// </summary>
    [DataField]
    public string State = string.Empty;
}

[Flags]
public enum ShotType
{
    Hitscan,
    Projectile
}
