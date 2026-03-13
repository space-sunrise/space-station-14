using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.NetTextures;

/// <summary>
/// Chunked fallback transport for NetTextures when high-bandwidth transfer is unavailable.
/// </summary>
public sealed class NetTextureResourceChunkMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public string RelativePath { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int TotalChunks { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        RelativePath = buffer.ReadString();
        ChunkIndex = buffer.ReadInt32();
        TotalChunks = buffer.ReadInt32();

        var dataLength = buffer.ReadInt32();
        Data = buffer.ReadBytes(dataLength);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(RelativePath);
        buffer.Write(ChunkIndex);
        buffer.Write(TotalChunks);
        buffer.Write(Data.Length);
        buffer.Write(Data);
    }
}
