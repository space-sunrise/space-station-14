using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.ServersHub
{
    [Serializable, NetSerializable]
    public sealed record ServerHubEntry(
        string Title,
        string StationName,
        string Preset,
        int CurrentPlayers,
        int MaxPlayers,
        string ConnectUrl,
        bool CanConnect);
}
