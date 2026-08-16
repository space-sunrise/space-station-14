using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Antag.Components;
using Content.Shared.Antag;
using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Content.Sunrise.Interfaces.Shared;
using Robust.Shared.Player;
using Robust.Shared.Random;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    /* Политика выбора Sunrise и приоритет спонсоров. */
    private ISharedSponsorsManager? _sponsorsManager;

    private void InitializeSunriseAntagSelection()
    {
        IoCManager.Instance!.TryResolveType(out _sponsorsManager);
    }

    private static bool CanSelectSunriseLateJoinAntag(PlayerSpawnCompleteEvent args)
    {
        return args.CanBeAntag;
    }

    private bool CanSelectSunriseCommandStaff(
        Entity<AntagSelectionComponent> ent,
        ICommonSession session,
        AntagSelectionDefinition definition)
    {
        if (!_jobs.IsCommandStaff(session))
            return true;

        if (!definition.PickCommandStaff)
            return false;

        if (definition.MaxCommandStaff == 0)
            return true;

        var selected = new HashSet<ICommonSession>(ent.Comp.AssignedSessions);
        foreach (var sessions in ent.Comp.PreSelectedSessions.Values)
        {
            foreach (var selectedSession in sessions)
            {
                selected.Add(selectedSession);
            }
        }

        var selectedCommandStaff = 0;
        foreach (var selectedSession in selected)
        {
            if (_jobs.IsCommandStaff(selectedSession))
                selectedCommandStaff++;
        }

        return selectedCommandStaff < definition.MaxCommandStaff;
    }

    private List<List<ICommonSession>> GetSunrisePlayerPools(
        Entity<AntagSelectionComponent> ent,
        IList<ICommonSession> sessions,
        AntagSelectionDefinition definition)
    {
        var preferred = new List<ICommonSession>();
        var fallback = new List<ICommonSession>();

        foreach (var session in sessions)
        {
            if (!IsSessionValid(ent, session, definition) || !IsEntityValid(session.AttachedEntity, definition))
                continue;

            if (ent.Comp.PreSelectedSessions.Values.Any(selected => selected.Contains(session)))
                continue;

            if (!CanSelectSunriseCommandStaff(ent, session, definition))
                continue;

            if (!ShouldCheckSunriseAntagPreference(definition) || ValidAntagPreference(session, definition.PrefRoles))
                preferred.Add(session);
            else if (ValidAntagPreference(session, definition.FallbackRoles))
                fallback.Add(session);
        }

        return [preferred, fallback];
    }

    private bool TryPickSunriseAntagSession(
        Entity<AntagSelectionComponent> ent,
        List<List<ICommonSession>> orderedPools,
        AntagSelectionDefinition definition,
        [NotNullWhen(true)] out ICommonSession? session)
    {
        session = null;

        foreach (var pool in orderedPools)
        {
            pool.RemoveAll(candidate => !CanSelectSunriseCommandStaff(ent, candidate, definition));
        }

        if (_sponsorsManager != null)
        {
            foreach (var preferenceRole in definition.PrefRoles)
            {
                foreach (var pool in orderedPools)
                {
                    var prioritySessions = _sponsorsManager.PickPrioritySessions(pool, preferenceRole);
                    if (prioritySessions.Count == 0)
                        continue;

                    var selected = RobustRandom.Pick(prioritySessions);
                    pool.Remove(selected);
                    session = selected;
                    return true;
                }
            }
        }

        foreach (var pool in orderedPools)
        {
            if (pool.Count == 0)
                continue;

            session = RobustRandom.PickAndTake(pool);
            return true;
        }

        return false;
    }

    private void RaiseSunriseAntagSelectionComplete(Entity<AntagSelectionComponent> ent)
    {
        var selectionComplete = new AntagSelectionCompleteEvent(ent);
        RaiseLocalEvent(ent, ref selectionComplete, true);
    }

    private static bool ShouldCheckSunriseAntagPreference(AntagSelectionDefinition definition)
    {
        return !definition.IgnorePrefCheck;
    }

    private static bool CanIgnoreSunriseAntagRestriction(AntagSelectionDefinition definition)
    {
        return definition.IgnoreCanBeAntag;
    }

    private static void TrackSunriseAntagSpawner(Entity<AntagSelectionComponent> ent)
    {
        ent.Comp.UseSpawners = true;
        ent.Comp.SpawnersCount++;
    }
}
