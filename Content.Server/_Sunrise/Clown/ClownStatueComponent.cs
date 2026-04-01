using Content.Server._Sunrise.Clown;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Clown;

[RegisterComponent, Access(typeof(ClownStatueSystem))]
public sealed partial class ClownStatueComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionClownSpawnStatue";

    [DataField]
    public EntityUid? ActionEntity;
}
