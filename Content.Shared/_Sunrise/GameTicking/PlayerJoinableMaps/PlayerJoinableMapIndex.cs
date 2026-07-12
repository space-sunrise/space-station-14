using System.Diagnostics.CodeAnalysis;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GameTicking.PlayerJoinableMaps;

/// <summary>
/// Indexes ownership of jobs by player-joinable maps without duplicating job-system state.
/// </summary>
public sealed class PlayerJoinableMapIndex
{
    private readonly List<PlayerJoinableMapPrototype> _maps = [];
    private readonly Dictionary<ProtoId<JobPrototype>, List<PlayerJoinableMapPrototype>> _mapsByJob = [];
    private readonly HashSet<ProtoId<JobPrototype>> _jobs = [];

    public IReadOnlyList<PlayerJoinableMapPrototype> Maps => _maps;
    public IReadOnlySet<ProtoId<JobPrototype>> Jobs => _jobs;

    /// <summary>
    /// Rebuilds the index from all registered player-joinable map prototypes.
    /// </summary>
    public void Rebuild(IPrototypeManager prototypeManager)
    {
        Rebuild(prototypeManager.EnumeratePrototypes<PlayerJoinableMapPrototype>());
    }

    /// <summary>
    /// Rebuilds the index from the provided map collection.
    /// </summary>
    public void Rebuild(IEnumerable<PlayerJoinableMapPrototype> maps)
    {
        _maps.Clear();
        _mapsByJob.Clear();
        _jobs.Clear();

        foreach (var map in maps)
        {
            _maps.Add(map);
            foreach (var job in map.Jobs)
            {
                _jobs.Add(job);
                if (!_mapsByJob.TryGetValue(job, out var jobMaps))
                {
                    jobMaps = [];
                    _mapsByJob.Add(job, jobMaps);
                }

                jobMaps.Add(map);
            }
        }

        _maps.Sort((left, right) => left.Order.CompareTo(right.Order));
    }

    /// <summary>
    /// Gets all player-joinable maps that declare the given job.
    /// </summary>
    public bool TryGetMaps(
        ProtoId<JobPrototype> job,
        [NotNullWhen(true)] out IReadOnlyList<PlayerJoinableMapPrototype>? maps)
    {
        if (_mapsByJob.TryGetValue(job, out var indexedMaps))
        {
            maps = indexedMaps;
            return true;
        }

        maps = null;
        return false;
    }

    /// <summary>
    /// Returns whether a job is unrestricted or has at least one available owner map.
    /// </summary>
    public bool IsJobAvailable(ProtoId<JobPrototype> job, Func<PlayerJoinableMapPrototype, bool> isMapAvailable)
    {
        if (!TryGetMaps(job, out var maps))
            return true;

        foreach (var map in maps)
        {
            if (isMapAvailable(map))
                return true;
        }

        return false;
    }
}
