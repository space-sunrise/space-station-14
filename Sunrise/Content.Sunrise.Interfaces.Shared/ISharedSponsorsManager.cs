using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Sunrise.Interfaces.Shared;

public interface ISharedSponsorsManager
{
    public void Initialize();

    public event Action? LoadedSponsorData;

    // Client
    public List<string> GetClientPrototypes();
    public bool ClientAllowedRespawn();

    public bool ClientIsSponsor();

    // Server
    public bool TryGetPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetOocTitle(NetUserId userId, [NotNullWhen(true)] out string? title);
    public bool TryGetOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public bool TryGetGhostTheme(NetUserId userId, [NotNullWhen(true)] out string? ghostTheme);
    public int GetExtraCharSlots(NetUserId userId);
    public bool HavePriorityJoin(NetUserId userId);
    public bool IsSponsor(NetUserId userId);
    public bool IsAllowedRespawn(NetUserId userId);
    public List<ICommonSession> PickPrioritySessions(List<ICommonSession> sessions, string roleId);
    public NetUserId? PickRoleSession(HashSet<NetUserId> users, string roleId);
    public bool TryGetPriorityGhostRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityAntags);
    public bool TryGetPriorityAntags(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityAntags);
    public bool TryGetPriorityRoles(NetUserId userId, [NotNullWhen(true)] out List<string>? priorityRoles);
}
