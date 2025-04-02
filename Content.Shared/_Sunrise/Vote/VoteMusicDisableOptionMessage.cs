using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Vote;

public sealed class VoteMusicDisableOptionMessage : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public bool Disable { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Disable = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Disable);
    }
}
