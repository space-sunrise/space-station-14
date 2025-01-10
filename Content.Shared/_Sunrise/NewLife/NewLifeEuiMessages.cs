using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.NewLife
{
    [NetSerializable, Serializable]
    public struct NewLifeCharacterInfo
    {
        public int Identifier { get; set; }
        public string Name { get; set; }
    }

    [NetSerializable, Serializable]
    public struct NewLifeRolesInfo
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
        public int? Count { get; set; }
    }

    [NetSerializable, Serializable]
    public sealed class NewLifeEuiState(
        List<NewLifeCharacterInfo> characters,
        Dictionary<NetEntity, string> stations,
        Dictionary<NetEntity, List<NewLifeRolesInfo>> jobs,
        TimeSpan nextRespawnTime,
        List<int> usedCharactersForRespawn)
        : EuiStateBase
    {
        public List<NewLifeCharacterInfo> Characters { get; } = characters;
        public Dictionary<NetEntity, string> Stations { get; } = stations;
        public Dictionary<NetEntity, List<NewLifeRolesInfo>> Jobs { get; } = jobs;
        public TimeSpan NextRespawnTime { get; } = nextRespawnTime;
        public List<int> UsedCharactersForRespawn { get; } = usedCharactersForRespawn;
    }

    [NetSerializable, Serializable]
    public sealed class NewLifeRequestSpawnMessage(int? characterId, NetEntity? stationId, string? roleProto)
        : EuiMessageBase
    {
        public int? CharacterId { get; } = characterId;
        public NetEntity? StationId { get; } = stationId;
        public string? RoleProto { get; } = roleProto;
    }
}
