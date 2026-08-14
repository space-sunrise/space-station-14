using Robust.Shared.Audio;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;

/// <summary>
///     Marker placed on the polymorph form created by Sanguine Pool.
///     Handles collision filtering on both client and server and exposes
///     tunables used while the form is active (trail spawning, etc.).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SanguinePoolComponent : Component
{
    /// <summary>
    ///     Prototype spawned when the pool enters a new tile
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? TrailPrototype = "PuddleBlood";

    [DataField, AutoNetworkedField]
    public EntProtoId ExitEffectPrototype = "VampireSanguinePoolIn";

    [DataField, AutoNetworkedField]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/vampire/exit_blood.ogg");

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> TrailReagent = "Blood";

    [DataField, AutoNetworkedField]
    public FixedPoint2 TrailReagentQuantity = FixedPoint2.New(30);

    [ViewVariables]
    public (EntityUid Grid, Vector2i Tile)? LastTrail;
}
