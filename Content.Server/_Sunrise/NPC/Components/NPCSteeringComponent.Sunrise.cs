using Content.Server.NPC.Components;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.NPC.Components;

public sealed partial class NPCSteeringComponent
{
    /// <summary>
    /// Whether to ignore pathing and just move directly to target.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DirectMove = false;

    /// <summary>
    /// Up to how fast can we be going before being considered in range, if not null.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float? InRangeMaxSpeed = null;
}
