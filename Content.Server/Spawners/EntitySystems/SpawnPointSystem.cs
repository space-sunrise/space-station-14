using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems;

public sealed class SpawnPointSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning);
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        if (args.SpawnResult != null)
            return;

        // TODO: Cache all this if it ends up important.
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();

        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
                continue;

            // Sunrise added start
            // Delta-V: Allow setting a desired SpawnPointType

            // То, что приходит из ивента главнее заданного в спавнпоинте.
            var spawnPointType = args.DesiredSpawnPointType != SpawnPointType.Unset
                ? args.DesiredSpawnPointType
                : spawnPoint.SpawnType;

            if (!IsMatchingSpawnPoint(spawnPoint, args.Job, spawnPointType))
                continue;

            possiblePositions.Add(xform.Coordinates);
            // Sunrise added end
        }

        if (possiblePositions.Count == 0)
        {
            // Ok we've still not returned, but we need to put them /somewhere/.
            // TODO: Refactor gameticker spawning code so we don't have to do this!
            var points2 = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

            if (points2.MoveNext(out _, out var xform))
            {
                Log.Error($"Unable to pick a valid spawn point, picking random spawner as a backup.\nRunLevel: {_gameTicker.RunLevel} Station: {ToPrettyString(args.Station)} Job: {args.Job}");
                possiblePositions.Add(xform.Coordinates);
            }
            else
            {
                Log.Error($"No spawn points were available!\nRunLevel: {_gameTicker.RunLevel} Station: {ToPrettyString(args.Station)} Job: {args.Job}");
                return;
            }
        }

        var spawnLoc = _random.Pick(possiblePositions);

        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }

    // Sunrise added start - общий API проверки spawnpoint без побочных эффектов
    /// <summary>
    /// Gets the spawn-point availability for a station and desired spawn-point type.
    /// </summary>
    public SpawnPointAvailability GetSpawnPointAvailability(EntityUid station, SpawnPointType spawnPointType)
    {
        if (spawnPointType == SpawnPointType.Unset)
            return SpawnPointAvailability.Unrestricted;

        HashSet<ProtoId<JobPrototype>>? jobs = null;
        var hasMatchingSpawnPoint = false;
        var hasUnrestrictedJobSpawnPoint = false;
        var points = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (points.MoveNext(out var uid, out var spawnPoint, out var xform))
        {
            if (_stationSystem.GetOwningStation(uid, xform) != station ||
                spawnPoint.SpawnType != spawnPointType)
            {
                continue;
            }

            hasMatchingSpawnPoint = true;
            if (spawnPointType != SpawnPointType.Job)
                continue;

            if (spawnPoint.Job is not { } job)
            {
                hasUnrestrictedJobSpawnPoint = true;
                continue;
            }

            jobs ??= [];
            jobs.Add(job);
        }

        return new SpawnPointAvailability(
            spawnPointType,
            hasMatchingSpawnPoint,
            hasUnrestrictedJobSpawnPoint,
            jobs);
    }

    private static bool IsMatchingSpawnPoint(
        SpawnPointComponent spawnPoint,
        ProtoId<JobPrototype>? job,
        SpawnPointType spawnPointType)
    {
        var isMatchingJob = job == null || spawnPoint.Job == null || spawnPoint.Job == job;
        return spawnPointType switch
        {
            SpawnPointType.Job => isMatchingJob && spawnPoint.SpawnType == SpawnPointType.Job,
            SpawnPointType.LateJoin => spawnPoint.SpawnType == SpawnPointType.LateJoin,
            SpawnPointType.Observer => spawnPoint.SpawnType == SpawnPointType.Observer,
            _ => false,
        };
    }
    // Sunrise added end
}

// Sunrise added start - результат общей проверки spawnpoint
/// <summary>
/// Describes spawn-point availability for a station and desired spawn-point type.
/// </summary>
public readonly record struct SpawnPointAvailability(
    SpawnPointType SpawnPointType,
    bool HasMatchingSpawnPoint,
    bool HasUnrestrictedJobSpawnPoint,
    IReadOnlySet<ProtoId<JobPrototype>>? Jobs)
{
    /// <summary>
    /// Availability result used when no specific spawn-point type is required.
    /// </summary>
    public static readonly SpawnPointAvailability Unrestricted =
        new(SpawnPointType.Unset, true, true, null);

    /// <summary>
    /// Returns whether the availability result supports the given job.
    /// </summary>
    public bool Matches(ProtoId<JobPrototype> job)
    {
        if (!HasMatchingSpawnPoint)
            return false;

        return SpawnPointType != SpawnPointType.Job ||
            HasUnrestrictedJobSpawnPoint ||
            Jobs?.Contains(job) == true;
    }
}
// Sunrise added end
