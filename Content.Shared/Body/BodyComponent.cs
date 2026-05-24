using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Body;

/// <summary>
/// Component on the entity that "has" a body, and that oversees entities with the <see cref="OrganComponent"/> inside it.
/// </summary>
/// <seealso cref="BodySystem" />
/// <seealso cref="SharedVisualBodySystem" />
[RegisterComponent, NetworkedComponent]
[Access(typeof(BodySystem))]
public sealed partial class BodyComponent : Component
{
    public const string ContainerID = "body_organs";

    // Sunrise-Edit start - legacy body prototype YAML compatibility
    /// <summary>
    /// Legacy pre-Nubody body prototype id from old downstream YAML.
    /// </summary>
    [DataField]
    public string? Prototype;

    /// <summary>
    /// Legacy pre-Nubody movement setting kept so old Body components deserialize.
    /// </summary>
    [DataField]
    public int RequiredLegs;
    // Sunrise-Edit end

    /// <summary>
    /// The actual container with entities with <see cref="OrganComponent" /> in it
    /// </summary>
    [ViewVariables]
    public Container? Organs;
}

/// <summary>
/// Raised on organ entity, when it is inserted into a body
/// </summary>
[ByRefEvent]
public readonly record struct OrganGotInsertedEvent(EntityUid Target);

/// <summary>
/// Raised on organ entity, when it is removed from a body
/// </summary>
[ByRefEvent]
public readonly record struct OrganGotRemovedEvent(EntityUid Target);

/// <summary>
/// Raised on body entity, when an organ is inserted into it
/// </summary>
[ByRefEvent]
public readonly record struct OrganInsertedIntoEvent(EntityUid Organ);

/// <summary>
/// Raised on body entity, when an organ is removed from it
/// </summary>
[ByRefEvent]
public readonly record struct OrganRemovedFromEvent(EntityUid Organ);
