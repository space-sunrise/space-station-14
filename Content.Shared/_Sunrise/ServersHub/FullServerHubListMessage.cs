using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.ServersHub;

public sealed class MsgFullServerHubList : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public List<ServerHubEntry> ServersHubEntries { get; set; } = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();
        ServersHubEntries.EnsureCapacity(count);
        for (var i = 0; i < count; i++)
        {
            ServersHubEntries.Add(new ServerHubEntry(
                buffer.ReadString(),
                buffer.ReadString(),
                buffer.ReadString(),
                buffer.ReadInt32(),
                buffer.ReadInt32(),
                buffer.ReadString(),
                buffer.ReadBoolean()
                ));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(ServersHubEntries.Count);
        foreach (var server in ServersHubEntries)
        {
            buffer.Write(server.Title);
            buffer.Write(server.StationName);
            buffer.Write(server.Preset);
            buffer.Write(server.CurrentPlayers);
            buffer.Write(server.MaxPlayers);
            buffer.Write(server.ConnectUrl);
            buffer.Write(server.CanConnect);
        }
    }
}
