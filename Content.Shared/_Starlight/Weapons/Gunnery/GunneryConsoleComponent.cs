using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Weapons.Gunnery;

/// <summary>
/// Marks an entity as a gunnery console - a targeting radar that can remotely aim and fire
/// shuttle-mounted cannons and guide EMP rockets.
/// </summary>
[RegisterComponent]
public sealed partial class GunneryConsoleComponent : Component
{
    /// <summary>
    /// Maximum targeting range in world units (metres).
    /// Default 512 covers most engagement distances for shuttle combat.
    /// </summary>
    [DataField]
    public float MaxRange = 512f;

    // Server-only runtime state.

    /// <summary>Server: EntityUid of the guided projectile currently being steered by this console, if any.</summary>
    public EntityUid? TrackedGuidedProjectile;

    /// <summary>Server: game time at which the last fire command was sent (used to associate spawned guided projectiles).</summary>
    public TimeSpan LastFireTime;

    /// <summary>Server: map-space position of the last fire target (used to immediately activate guided projectile steering).</summary>
    public Vector2 LastFireTargetPos;

    /// <summary>
    /// Server: cannons that should continue firing while the user holds LMB on the console.
    /// </summary>
    public Dictionary<EntityUid, EntityCoordinates> HeldCannons = new();

    /// <summary>
    /// Server: set after LMB release. Full-auto stops immediately, burst is allowed to finish.
    /// </summary>
    public bool ReleaseRequested;
}

/// <summary>
/// UI key for <see cref="GunneryConsoleComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public enum GunneryConsoleUiKey : byte
{
    Key,
}
