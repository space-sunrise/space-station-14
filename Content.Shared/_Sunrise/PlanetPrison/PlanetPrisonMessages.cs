using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PlanetPrison;

[NetSerializable, Serializable]
public sealed class PlanetPrisonVoteMessage : EntityEventArgs
{
    public string MapId { get; }
    public int Priority { get; } // 0 = Never, 1 = Low, 2 = Medium, 3 = High

    public PlanetPrisonVoteMessage(string mapId, int priority)
    {
        MapId = mapId;
        Priority = priority;
    }
}

[NetSerializable, Serializable]
public sealed class PlanetPrisonVoteUpdateEvent : EntityEventArgs
{
    public string MapId { get; }
    public int VoteCount { get; } // Количество голосов с приоритетом > 0
    public bool IsVoting { get; } // Идет ли голосование
    public int? RemainingSeconds { get; } // Оставшееся время голосования
    public int TotalPriorityPlayers { get; } // Общее количество игроков с приоритетами
    public int MinPlayersRequired { get; } // Минимальное количество игроков для запуска
    public bool IsLaunched { get; } // Запущена ли карта

    public PlanetPrisonVoteUpdateEvent(string mapId, int voteCount, bool isVoting, int? remainingSeconds = null, int totalPriorityPlayers = 0, int minPlayersRequired = 2, bool isLaunched = false)
    {
        MapId = mapId;
        VoteCount = voteCount;
        IsVoting = isVoting;
        RemainingSeconds = remainingSeconds;
        TotalPriorityPlayers = totalPriorityPlayers;
        MinPlayersRequired = minPlayersRequired;
        IsLaunched = isLaunched;
    }
}

[NetSerializable, Serializable]
public sealed class PlanetPrisonStatusRequestMessage : EntityEventArgs
{
    public string MapId { get; }
    public PlanetPrisonStatusRequestMessage(string mapId)
    {
        MapId = mapId;
    }
}

[NetSerializable, Serializable]
public sealed class PlanetPrisonMapRemovedMessage : EntityEventArgs
{
    public string ProtoId { get; }
    public PlanetPrisonMapRemovedMessage(string protoId)
    {
        ProtoId = protoId;
    }
}

[NetSerializable, Serializable]
public sealed class PlanetPrisonRolePriorityMessage : EntityEventArgs
{
    public string RoleId { get; }
    public int Priority { get; } // 0 = Never, 1 = Low, 2 = Medium, 3 = High

    public PlanetPrisonRolePriorityMessage(string roleId, int priority)
    {
        RoleId = roleId;
        Priority = priority;
    }
}

[NetSerializable, Serializable]
public sealed class PlanetPrisonRoleUpdateEvent : EntityEventArgs
{
    public string RoleId { get; }
    public bool IsTaken { get; } // Роль занята другим игроком
    public bool IsAssigned { get; } // Роль назначена этому игроку

    public PlanetPrisonRoleUpdateEvent(string roleId, bool isTaken, bool isAssigned)
    {
        RoleId = roleId;
        IsTaken = isTaken;
        IsAssigned = isAssigned;
    }
}

[NetSerializable, Serializable]
public sealed class PlanetPrisonCloseWindowEvent : EntityEventArgs
{
}
