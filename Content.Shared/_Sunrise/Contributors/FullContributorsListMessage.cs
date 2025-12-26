using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Contributors;

public sealed class MsgFullContributorsList : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public List<ContributorEntry> ContributorsEntries { get; set; } = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();
        ContributorsEntries.EnsureCapacity(count);
        for (var i = 0; i < count; i++)
        {
            ContributorsEntries.Add(new ContributorEntry(
                buffer.ReadInt32(),
                buffer.ReadString(),
                buffer.ReadString(),
                buffer.ReadString(),
                buffer.ReadInt32()
                ));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(ContributorsEntries.Count);
        foreach (var contributor in ContributorsEntries)
        {
            buffer.Write(contributor.GithubId);
            buffer.Write(contributor.GithubLogin);
            buffer.Write(contributor.SS14UserId);
            buffer.Write(contributor.SS14Username);
            buffer.Write(contributor.Contributions);
        }
    }
}
