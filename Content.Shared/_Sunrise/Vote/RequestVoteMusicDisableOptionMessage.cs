using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Vote;

public sealed class RequestVoteMusicDisableOptionMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
    }
}
