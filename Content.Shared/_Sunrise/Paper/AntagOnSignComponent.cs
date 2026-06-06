using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Paper;

[RegisterComponent]
public sealed partial class AntagOnSignComponent : Component
{
    [DataField("charges")]
    public int ChargesRemaining = 1;

    [ViewVariables]
    public List<EntityUid> SignedEntityUids = [];

    [DataField]
    public float Chance = 1.0f;

    [DataField("spawnParadoxClone")]
    public bool ParadoxClone = false;

    [DataField]
    public List<AntagCompPair> Antags = [];

    [DataField]
    public EntityWhitelist? Blacklist;
}

[DataDefinition]
public sealed partial class AntagCompPair
{
    [DataField]
    public EntProtoId Antag;

    [DataField]
    public string TargetComponent = string.Empty;
}
