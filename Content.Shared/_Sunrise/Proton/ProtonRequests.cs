using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Proton;

/// <summary>
/// Request client to send screenshot
/// </summary>
public sealed class ProtonRequestScreenshotClient : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
    }
}

/// <summary>
/// Send request for a screenshot of screen of a specific player
/// </summary>
public sealed class ProtonRequestScreenshotServer : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    // Data sent
    public string? Target;

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Target);
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Target = buffer.ReadString();
    }
}
