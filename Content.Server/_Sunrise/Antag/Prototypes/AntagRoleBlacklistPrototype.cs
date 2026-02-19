using System.Collections.Generic;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Antag.Prototypes;

[Prototype]
public sealed partial class AntagRoleBlacklistPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntityWhitelist Blacklist = default!;

    [DataField(required: true)]
    public HashSet<EntProtoId> MindRoles = new();
}
