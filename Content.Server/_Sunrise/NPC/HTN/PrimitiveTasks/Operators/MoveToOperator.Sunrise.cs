// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class MoveToOperator
{
    /// <summary>
    /// Velocity below which we count as successfully braked.
    /// Don't care about velocity if null.
    /// </summary>
    [DataField]
    public float? BrakeMaxVelocity = 0.03f;

    /// <summary>
    /// If either we or the target are offgrid, gets assigned to make us just move directly to target without pathfinding.
    /// </summary>
    [DataField]
    public string DirectMoveTargetKey = "DirectMoveTarget";
}
