using System.Numerics;
using Content.Shared.Construction.Conditions;

namespace Content.Shared._Sunrise.Tutorial.Components;

/// <summary>
/// Runtime state for the shared map that hosts loaded tutorial grids.
/// </summary>
[RegisterComponent]
public sealed partial class TutorialMapComponent : Component
{
    /// <summary>
    /// Tutorial grids currently loaded on this map.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> LoadedGrids = [];

    /// <summary>
    /// Map-space offset assigned to each loaded tutorial grid.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, Vector2> GridOffsets = new();

    /// <summary>
    /// Name assigned to the tutorial map for debugging/admin visibility.
    /// </summary>
    [ViewVariables]
    public string MapName = "Tutorial Map (DO NOT TOUCH)";

    /// <summary>
    /// Offset added between loaded tutorial grids to keep simultaneous sessions apart.
    /// </summary>
    [ViewVariables]
    public Vector2 CoordinateStep = new(0, 200);
}
