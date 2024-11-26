using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Content.Sunrise.Interfaces.Shared;

namespace Content.Server._Sunrise.SponsorLoadout;

public sealed class SponsorLoadoutSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationSpawningSystem _spawn = default!;
    private ISharedSponsorsManager? _sponsorsManager;

    public override void Initialize()
    {
        IoCManager.Instance!.TryResolveType(out _sponsorsManager);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (_sponsorsManager == null)
            return;

        if (!_sponsorsManager.TryGetPrototypes(ev.Player.UserId, out var prototypes))
            return;
        foreach (var loadoutId in prototypes)
        {
            if (!_prototypeManager.TryIndex<SponsorLoadoutPrototype>(loadoutId, out var loadout))
                continue;
            var isSponsorHave = !prototypes.Contains(loadoutId);
            var isWhitelisted = ev.JobId != null &&
                                loadout.WhitelistJobs != null &&
                                !loadout.WhitelistJobs.Contains(ev.JobId);
            var isBlacklisted = ev.JobId != null &&
                                loadout.BlacklistJobs != null &&
                                loadout.BlacklistJobs.Contains(ev.JobId);
            var isSpeciesRestricted = loadout.SpeciesRestrictions != null &&
                                      loadout.SpeciesRestrictions.Contains(ev.Profile.Species);

            if (isSponsorHave || isWhitelisted || isBlacklisted || isSpeciesRestricted)
                continue;

            if (!_prototypeManager.TryIndex(loadout.Equipment, out var startingGear))
                continue;

            _spawn.EquipStartingGear(ev.Mob, startingGear);
        }
    }
}
