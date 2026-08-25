using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Antags.Vampires.Components;

/// <summary>
///     Маркерный компонент для сущностей, порабощённых вампиром Данталион.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class VampireThrallComponent : BaseMindRoleComponent
{
    /// <summary>
    ///     Вампир, который сейчас контролирует этого тхралла
    /// </summary>
    public EntityUid? Master;

    [DataField]
    public FixedPoint2 HolyWaterConsumed = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 HolyWaterToBreakFree = FixedPoint2.New(30);

    [DataField]
    public ProtoId<ReagentPrototype> HolyWaterReagentId = "Holywater";

    [DataField]
    public TimeSpan DeconvertStunDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan BreakFreeCheckInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public EntProtoId ObeyObjectiveId = "VampireThrallObeyMasterObjective";

    [DataField]
    public EntProtoId MindRoleId = "MindRoleThrall";

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan NextBreakFreeCheck;
}
