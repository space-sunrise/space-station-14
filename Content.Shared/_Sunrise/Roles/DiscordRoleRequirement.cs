using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class DiscordRoleRequirement : JobRequirement
{
    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        string? protoId, // Sunrise-Edit
        string[] sponsorPrototypes, // Sunrise-Edit
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        if (profile is null)
            return true;

        // Sunrise-Sponsors-Start
        if (sponsorPrototypes.Contains(protoId))
            return true;
        // Sunrise-Sponsors-End

        reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-discord"));
        return false;
    }
}
