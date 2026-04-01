using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Clown;

[RegisterComponent, Access(typeof(ClownStandSystem))]
public sealed partial class ClownStandComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionClownStandToggle";

    [DataField]
    public EntProtoId StandPrototype = "MobClownHulk";
}
