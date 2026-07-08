using Content.Shared.Roles;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

[Prototype]
public sealed partial class PlayerJoinableMapPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId DisplayName;

    [DataField]
    public string? PlayerAccessEnabledCVar;

    [DataField]
    public string? PlayerAccessMinPlayersCVar;

    [DataField]
    public bool SpawnWhenPlayerAccessDisabled;

    [DataField]
    public int Order;

    [DataField]
    public HashSet<ProtoId<JobPrototype>> Jobs = [];
}
