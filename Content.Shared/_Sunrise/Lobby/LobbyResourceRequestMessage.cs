using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Lobby;

/// <summary>
/// Message sent from client to server to request a lobby resource (animation or art).
/// </summary>
public sealed class RequestLobbyResourceMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    /// <summary>
    /// Type of resource being requested.
    /// </summary>
    public LobbyResourceType ResourceType { get; set; }

    /// <summary>
    /// ID of the resource prototype (LobbyAnimationPrototype.ID or LobbyBackgroundPrototype.ID).
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        ResourceType = (LobbyResourceType)buffer.ReadByte();
        ResourceId = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write((byte)ResourceType);
        buffer.Write(ResourceId);
    }
}

/// <summary>
/// Type of lobby resource.
/// </summary>
public enum LobbyResourceType : byte
{
    Animation,
    Art
}

