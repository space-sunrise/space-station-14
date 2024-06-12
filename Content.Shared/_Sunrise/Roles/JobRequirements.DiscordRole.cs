using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Roles
{
    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class DiscordRoleRequirement : JobRequirement
    {
    }
}
