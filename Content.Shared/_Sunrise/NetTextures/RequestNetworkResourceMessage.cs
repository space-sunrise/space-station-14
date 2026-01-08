using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.NetTextures;

/// <summary>
/// Message sent from client to server to request a network resource by path.
/// The resource will be loaded into MemoryContentRoot on the client.
/// </summary>
public sealed class RequestNetworkResourceMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    /// <summary>
    /// Path to the resource to request (e.g., "/NetTextures/Lobby/Animations/bar.rsi" or "NetTextures/Lobby/Arts/image.webp").
    /// Can be absolute (starting with /) or relative to Resources root.
    /// </summary>
    public string ResourcePath { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        ResourcePath = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(ResourcePath);
    }
}





