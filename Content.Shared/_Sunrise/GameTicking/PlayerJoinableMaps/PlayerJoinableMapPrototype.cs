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
    public PlayerJoinableMapAccessType Access = PlayerJoinableMapAccessType.Always;

    [DataField]
    public bool SpawnWhenPlayerAccessDisabled;

    [DataField]
    public bool ExcludeFromFallbackSpawn = true;

    [DataField]
    public PlayerJoinableMapSpawnPointType LateJoinSpawnPointType = PlayerJoinableMapSpawnPointType.Job;

    [DataField]
    public PlayerJoinableMapSpawnPointType RoundStartSpawnPointType = PlayerJoinableMapSpawnPointType.Job;

    [DataField]
    public int Order;

    [DataField]
    public HashSet<ProtoId<JobPrototype>> Jobs = [];
}

public enum PlayerJoinableMapSpawnPointType
{
    Unset,
    Job,
}

public enum PlayerJoinableMapAccessType
{
    Always,
    CentComm,
    PlanetPrison,
}
