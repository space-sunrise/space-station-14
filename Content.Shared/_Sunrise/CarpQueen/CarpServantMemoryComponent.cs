using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CarpQueen;

/// <summary>
/// Component that stores memory of the liquid the carp hatched from,
/// including its color and reagents for injection on bite.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CarpServantMemoryComponent : Component
{
    /// <summary>
    /// Color of the liquid the carp hatched from.
    /// Used for visual appearance.
    /// </summary>
    [DataField("liquidColor"), AutoNetworkedField]
    public Color LiquidColor = Color.White;

    /// <summary>
    /// Dictionary of reagent IDs and their amounts that were in the liquid.
    /// Used for injection on bite.
    /// </summary>
    [DataField("rememberedReagents"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> RememberedReagents = new();

    /// <summary>
    /// Amount of each remembered reagent to inject per bite (in units).
    /// </summary>
    [DataField("biteReagentAmount")]
    public FixedPoint2 BiteReagentAmount = FixedPoint2.New(1);

    /// <summary>
    /// List of players that were nearby when the carp hatched.
    /// These players are considered "friends" and won't be attacked
    /// unless the queen orders it.
    /// </summary>
    [DataField("rememberedFriends"), AutoNetworkedField]
    public HashSet<EntityUid> RememberedFriends = new();

    /// <summary>
    /// List of entities that the carp is temporarily forbidden to attack.
    /// These are cleared when the attacker damages the carp's owner.
    /// </summary>
    [DataField("forbiddenTargets")]
    public HashSet<EntityUid> ForbiddenTargets = new();
}

