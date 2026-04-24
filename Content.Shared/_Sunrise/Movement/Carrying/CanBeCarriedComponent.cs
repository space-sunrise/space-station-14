using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Movement.Carrying;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanBeCarriedComponent : Component
{
    /// <summary>
    /// Carriers allowed to carry this entity. Null means any carrier can pass this filter.
    /// </summary>
    [DataField]
    public EntityWhitelist? CarrierWhitelist;

    /// <summary>
    /// Carriers forbidden to carry this entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? CarrierBlacklist;

    /// <summary>
    /// Number of free hands required to carry this entity.
    /// </summary>
    [DataField]
    public int FreeHandsRequired = 2;

    /// <summary>
    /// Walking speed modifier applied when pulling this entity.
    /// </summary>
    [DataField]
    public float PullWalkSpeedModifier = 0.6f;

    /// <summary>
    /// Sprinting speed modifier applied when pulling this entity.
    /// </summary>
    [DataField]
    public float PullSprintSpeedModifier = 0.6f;

    /// <summary>
    /// Movement speed modifier applied to the carrier while carrying this entity.
    /// </summary>
    [DataField]
    public float CarrierSpeedModifier = 0.6f;

    /// <summary>
    /// Movement speed modifier applied to the carrier when this entity is critical or dead.
    /// </summary>
    [DataField]
    public float IncapacitatedCarrierSpeedModifier = 0.8f;

    /// <summary>
    /// Pickup time multiplier applied while this entity is not knocked down.
    /// </summary>
    [DataField]
    public float StandingPickupTimeMultiplier = 2f;

    /// <summary>
    /// Pickup time multiplier applied while this entity is critical or dead.
    /// </summary>
    [DataField]
    public float IncapacitatedPickupTimeMultiplier = 0.5f;

    [DataField]
    public SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"));
}
