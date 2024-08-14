using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Sunrise.Interfaces.Shared;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Localization;

namespace Content.Shared._Sunrise.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class DiscordRoleRequirement : JobRequirement
{
    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        ISharedSponsorsManager? sponsorsManager, // Sunrise-Edit
        string? protoId, // Sunrise-Edit
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (profile is null)
            return true;

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-discord"));
        return false;
    }
}
