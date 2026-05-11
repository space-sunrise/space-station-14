using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server._Sunrise.Mapping;

/// <summary>
/// Content-side rules for replacing already placed entities during mapper placement.
/// </summary>
[RegisterComponent, Access(typeof(MappingReplacementSystem))]
public sealed partial class MappingReplacementComponent : Component
{
    /// <summary>
    /// Shared replacement group. An empty value falls back to the placed prototype ID.
    /// </summary>
    [DataField]
    public string Key = string.Empty;

    /// <summary>
    /// Uses the prototype ID as the replacement key even when <see cref="Key"/> is set.
    /// </summary>
    [DataField]
    public bool UsePrototypeId;

    /// <summary>
    /// Requires matching rotation before one mapped entity can replace another.
    /// </summary>
    [DataField]
    public bool RequireSameRotation;
}
