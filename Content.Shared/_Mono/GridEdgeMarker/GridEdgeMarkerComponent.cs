using System.Numerics;

namespace Content.Shared._Mono.GridEdgeMarker;

/// <summary>
///     Causes this entity to display as a grid edge on radar interfaces, even inside grids.
/// </summary>
[RegisterComponent]
public sealed partial class GridEdgeMarkerComponent : Component
{
    /// <summary>
    ///     Start of the edge segment, in tile-local coordinates relative to the entity origin.
    ///     Rotated by the entity's local rotation and scaled by grid tile size.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Begin;

    /// <summary>
    ///     End of the edge segment. Same coordinate space as <see cref="Begin"/>.
    /// </summary>
    [DataField(required: true)]
    public Vector2 End;
}
