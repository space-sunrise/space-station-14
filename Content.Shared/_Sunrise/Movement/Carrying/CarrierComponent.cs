using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Carrying;

/// <summary>
/// Marks an entity as able to pick up and hold entities with <see cref="CanBeCarriedComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CarrierComponent : Component
{
    /// <summary>
    /// Entities this carrier is allowed to carry. Null means any target can pass this filter.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    /// <summary>
    /// Entities this carrier is forbidden to carry.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist;

    /// <summary>
    /// Maximum distance to the target required to start carrying.
    /// </summary>
    [DataField]
    public float InteractionRange = 0.75f;

    /// <summary>
    /// Maximum distance between carrier and target before active carrying is dropped.
    /// </summary>
    [DataField]
    public float MaxSeparation = 0.1f;

    /// <summary>
    /// Vertical world offset from carrier center where the carried entity is held.
    /// </summary>
    [DataField]
    public float CarriedOffset = 0.3f;

    /// <summary>
    /// Base duration of the pickup action before mass and target state modifiers are applied.
    /// </summary>
    [DataField]
    public TimeSpan BasePickupTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Pickup attempts longer than this are rejected as too heavy.
    /// </summary>
    [DataField]
    public TimeSpan MaxPickupTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Movement allowed during the pickup action before the action is interrupted.
    /// </summary>
    [DataField]
    public float PickupMovementThreshold = 0.01f;

    /// <summary>
    /// Divisor used to convert throw speed into throw distance.
    /// </summary>
    [DataField]
    public float ThrowGravity = 8f;

    /// <summary>
    /// Multiplier applied to the regular throw speed when throwing a carried entity.
    /// </summary>
    [DataField]
    public float ThrowSpeedModifier = 0.36f;

    /// <summary>
    /// How strongly the carrier-to-target mass ratio affects throw speed.
    /// </summary>
    [DataField]
    public float ThrowMassExponent = 0.25f;

    /// <summary>
    /// Minimum throw speed for carried entities.
    /// </summary>
    [DataField]
    public float MinThrowSpeed = 1f;

    /// <summary>
    /// Maximum throw speed for carried entities.
    /// </summary>
    [DataField]
    public float MaxThrowSpeed = 4.5f;

    /// <summary>
    /// Minimum throw distance for carried entities.
    /// </summary>
    [DataField]
    public float MinThrowDistance = 0.25f;

    /// <summary>
    /// Maximum throw distance for carried entities.
    /// </summary>
    [DataField]
    public float MaxThrowDistance = 2f;

    /// <summary>
    /// How strongly the carrier-to-target mass ratio affects carrier slowdown.
    /// </summary>
    [DataField]
    public float MassSlowdownInfluence = 0.1f;

    /// <summary>
    /// Minimum mass-based multiplier applied to carrier slowdown.
    /// </summary>
    [DataField]
    public float MinMassSlowdownModifier = 0.5f;

    /// <summary>
    /// Maximum mass-based multiplier applied to carrier slowdown.
    /// </summary>
    [DataField]
    public float MaxMassSlowdownModifier = 1.2f;

    /// <summary>
    /// Minimum final movement speed modifier while carrying.
    /// </summary>
    [DataField]
    public float MinSpeedModifier = 0.1f;
}
