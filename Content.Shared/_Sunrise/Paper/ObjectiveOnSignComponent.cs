using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Paper;

[RegisterComponent]
public sealed partial class ObjectiveOnSignComponent : Component
{
    [DataField("charges")]
    public int ChargesRemaining = 1;

    [ViewVariables]
    public List<EntityUid> SignedEntityUids = [];

    [DataField]
    public float Chance = 1.0f;

    [DataField]
    public List<EntProtoId> Objectives = [];

    [DataField]
    public bool Append = false;

    [DataField]
    public EntityWhitelist? Whitelist = null;

    [DataField]
    public EntityWhitelist? Blacklist = null;
}
