using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.CarpQueen;

[RegisterComponent, NetworkedComponent]
public sealed partial class CarpEggComponent : Component
{
    [DataField("queen")] public EntityUid? Queen;

    /// <summary>
    /// Required puddle volume (u) to hatch.
    /// </summary>
    [DataField("requiredVolume")] public float RequiredVolume = 10f;

    /// <summary>
    /// Seconds between hatch checks.
    /// </summary>
    [DataField("checkInterval")] public float CheckInterval = 3f;

    [DataField("accum")] public float Accum;

    /// <summary>
    /// Seconds the egg must remain on valid liquid before hatching.
    /// </summary>
    [DataField("hatchDelay")] public float HatchDelay = 5f;

    /// <summary>
    /// Whether current tile conditions are sufficient for hatching.
    /// </summary>
    [DataField("eligible")] public bool Eligible;

    /// <summary>
    /// Accumulated time spent waiting without valid liquid. If exceeds MaxWaitWithoutLiquid, egg breaks.
    /// </summary>
    [DataField("waitElapsed")] public float WaitElapsed;

    /// <summary>
    /// Max seconds to wait for liquid to appear before breaking the egg.
    /// </summary>
    [DataField("maxWaitWithoutLiquid")] public float MaxWaitWithoutLiquid = 30f;

    /// <summary>
    /// Range (in tiles) to search for queen when hatching.
    /// If queen is within this range, carp becomes servant; otherwise, it imprints on nearby players.
    /// </summary>
    [DataField("queenSearchRange")] public float QueenSearchRange = 3f;

    /// <summary>
    /// Range (in tiles) to search for nearby players to imprint on when queen is not nearby.
    /// Increased to allow carps to remember friends from further away.
    /// </summary>
    [DataField("friendSearchRange")] public float FriendSearchRange = 6f;

    /// <summary>
    /// Amount of reagent injected per carp bite.
    /// </summary>
    [DataField("biteReagentAmount")] public FixedPoint2 BiteReagentAmount = FixedPoint2.New(1);
}


