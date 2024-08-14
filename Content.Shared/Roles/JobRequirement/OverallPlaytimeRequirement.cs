using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Sunrise.Interfaces.Shared;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class OverallPlaytimeRequirement : JobRequirement
{
    /// <inheritdoc cref="DepartmentTimeRequirement.Time"/>
    [DataField(required: true)]
    public TimeSpan Time;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        ISharedSponsorsManager? sponsorsManager, // Sunrise-Edit
        string? protoId, // Sunrise-Edit
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = new FormattedMessage();

        // Sunrise-Sponsors-Start
        if (sponsorsManager != null && protoId != null && sponsorsManager.GetClientPrototypes().Contains(protoId))
            return true;
        // Sunrise-Sponsors-End

        var overallTime = playTimes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
        var overallDiff = Time.TotalMinutes - overallTime.TotalMinutes;

        if (!Inverted)
        {
            if (overallDiff <= 0 || overallTime >= Time)
                return true;

            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString(
                "role-timer-overall-insufficient",
                ("time", Math.Ceiling(overallDiff))));
            return false;
        }

        if (overallDiff <= 0 || overallTime >= Time)
        {
            reason = FormattedMessage.FromMarkupPermissive(Loc.GetString("role-timer-overall-too-high",
                ("time", -overallDiff)));
            return false;
        }

        return true;
    }
}
