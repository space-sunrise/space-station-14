using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public static class JobRequirements
{
    public static bool TryRequirementsMet(
        JobPrototype job,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason,
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        ISharedSponsorsManager? sponsorsManager) // Sunrise-Edit
    {
        var sys = entManager.System<SharedRoleSystem>();
        var requirements = sys.GetJobRequirement(job);
        reason = null;
        if (requirements == null)
            return true;

        foreach (var requirement in requirements)
        {
            if (!requirement.Check(entManager, protoManager, sponsorsManager, job.ID, profile, playTimes, out reason)) // Sunrise-Edit
                return false;
        }

        return true;
    }
}

/// <summary>
/// Abstract class for playtime and other requirements for role gates.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class JobRequirement
{
    [DataField]
    public bool Inverted;

    public abstract bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        ISharedSponsorsManager? sponsorsManager, // Sunrise-Edit
        string? protoId, // Sunrise-Edit
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason);
}
