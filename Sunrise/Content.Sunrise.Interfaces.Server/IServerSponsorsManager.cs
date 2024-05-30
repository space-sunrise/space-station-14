using System.Diagnostics.CodeAnalysis;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Sunrise.Interfaces.Server;

public interface IServerSponsorsManager : ISharedSponsorsManager
{
    public bool TryGetGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme);
    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public bool TryGetOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title);
    public int GetExtraCharSlots(NetUserId userId);
    public bool HavePriorityJoin(NetUserId userId);
    public bool IsSponsor(NetUserId userId);
    public bool AllowedRespawn(NetUserId userId);
    public List<ICommonSession> PickPrioritySessions(List<ICommonSession> sessions, string roleId);
    public NetUserId PickRoleSession(HashSet<NetUserId> users, string roleId);
}
