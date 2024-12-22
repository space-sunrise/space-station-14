using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.GhostTheme;

public sealed class MsgGhostThemeSend : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public string GhostTheme { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        GhostTheme = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(GhostTheme);
    }
}
