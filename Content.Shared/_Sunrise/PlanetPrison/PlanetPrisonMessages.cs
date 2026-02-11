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
    [DataField]
    public string MapId { get; set; } = default!;
    [DataField]
    public int VoteCount { get; set; } // Количество голосов с приоритетом > 0
    [DataField]
    public bool IsVoting { get; set; } // Идет ли голосование
    [DataField]
    public double RemainingSeconds { get; set; } // Оставшееся время голосования
    [DataField]
    public int TotalPriorityPlayers { get; set; } // Общее количество игроков с приоритетами
    [DataField]
    public bool IsLaunched { get; set; } // Запущена ли карта
    [DataField]
    public string[] InsufficientRoles { get; set; } = Array.Empty<string>();
    [DataField]
    public bool IsHidden { get; set; } // Карта скрыта (кэшированная)
    [DataField]
    public int MinPlayers { get; set; } // Минимальное количество игроков для запуска карты

    public PlanetPrisonVoteUpdateEvent() { }

    public PlanetPrisonVoteUpdateEvent(string mapId, int voteCount, bool isVoting, bool isLaunched, double remainingSeconds, int totalPriorityPlayers, string[] insufficientRoles, int minPlayers = 2, bool isHidden = false)
    {
        MapId = mapId;
        VoteCount = voteCount;
        IsVoting = isVoting;
        IsLaunched = isLaunched;
        RemainingSeconds = remainingSeconds;
        TotalPriorityPlayers = totalPriorityPlayers;
        InsufficientRoles = insufficientRoles ?? Array.Empty<string>();
        MinPlayers = minPlayers;
        IsHidden = isHidden;
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
