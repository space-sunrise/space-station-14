using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Boss.Systems;

/// <summary>
///     This handles...
/// </summary>
public abstract class SharedHellSpawnArenaSystem : EntitySystem
{
    public static HellSpawnBossStatus Status = HellSpawnBossStatus.Idle;
    public EntityUid? Arena;
    public MapId? ArenaMap;

    public TimeSpan CooldownLength = TimeSpan.FromSeconds(3);
    public HashSet<EntityUid> MarkedTargets = new();
    public ResPath ShuttlePath = new("/Maps/_Sunrise/arena.yml");
    public List<EntityUid> Shuttles = [];

    /// <inheritdoc />
    public override void Initialize()
    {
    }
}

[Serializable] [NetSerializable]
public sealed class HellSpawnArenaConsoleUiState : BoundUserInterfaceState
{
    public TimeSpan? ActivationTime;
    public HellSpawnBossStatus CurrentStatus;
}

[Serializable] [NetSerializable]
public enum HellSpawnBossStatus : byte
{
    Idle,
    InProgress,
    Cooldown,
}

[Serializable] [NetSerializable]
public sealed class TravelButtonPressedMessage : BoundUserInterfaceMessage
{
    public NetEntity Owner;
}

public sealed class TravelModeToggledEvent : EntityEventArgs
{
    public bool Enabled = false;
}

[Serializable] [NetSerializable]
public enum HellSpawnArenaUiKey
{
    Key,
}
