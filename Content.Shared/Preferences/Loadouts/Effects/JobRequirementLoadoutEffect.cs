using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Checks for a job requirement to be met such as playtime.
/// </summary>
public sealed partial class JobRequirementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public JobRequirement Requirement = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (session == null)
        {
            reason = FormattedMessage.Empty;
            return true;
        }

        // Sunrise-Sponsors-Start
        string[] sponsorPrototypes = [];
        if (collection.TryResolveType<ISharedSponsorsManager>(out var sponsorsManager))
        {
            sponsorPrototypes = sponsorsManager?.GetClientPrototypes().ToArray() ?? [];
        }
        // Sunrise-Sponsors-End

        var manager = collection.Resolve<ISharedPlaytimeManager>();
        var playtimes = manager.GetPlayTimes(session);

        return Requirement.Check(collection.Resolve<IEntityManager>(),
            collection.Resolve<IPrototypeManager>(),
            profile,
            playtimes,
            null, // Sunrise-Sponsors
            sponsorPrototypes, // Sunrise-Sponsors
            out reason);
    }
}
