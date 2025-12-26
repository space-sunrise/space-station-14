using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PlayerCache;

public sealed class MsgPlayerCacheSync : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;
    public PlayerCacheData Cache { get; set; } = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var length = buffer.ReadVariableInt32();
        var data = buffer.ReadBytes(length);

        using var stream = new MemoryStream(data);
        serializer.DeserializeDirect(stream, out PlayerCacheData? cache);
        Cache = cache ?? new PlayerCacheData();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        using var stream = new MemoryStream();
        serializer.SerializeDirect(stream, Cache);

        var data = stream.ToArray();
        buffer.WriteVariableInt32(data.Length);
        buffer.Write(data);
    }
}
