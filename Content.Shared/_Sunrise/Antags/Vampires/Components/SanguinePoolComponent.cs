using Robust.Shared.Audio;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components;

/// <summary>
///     Маркер на форме-полиморфе, созданной Кровавой лужей.
///     Обрабатывает фильтрацию коллизий на клиенте и сервере и предоставляет
///     настраиваемые параметры, используемые пока активна форма (спавн следа и т.п.).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SanguinePoolComponent : Component
{
    /// <summary>
    ///     Прототип, спавнящийся при входе лужи на новый тайл
    /// </summary>
    [DataField]
    public EntProtoId? TrailPrototype = "PuddleBlood";

    [DataField]
    public EntProtoId ExitEffectPrototype = "VampireSanguinePoolIn";

    [DataField]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/_Sunrise/Effects/vampire/exit_blood.ogg");

    [DataField]
    public ProtoId<ReagentPrototype> TrailReagent = "Blood";

    [DataField]
    public FixedPoint2 TrailReagentQuantity = FixedPoint2.New(30);

    [ViewVariables]
    public (EntityUid Grid, Vector2i Tile)? LastTrail;
}
