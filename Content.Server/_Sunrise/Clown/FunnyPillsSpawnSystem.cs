using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Clown;

/// <summary>
/// Gives every spawning player a canister of funny pills.
/// </summary>
public sealed class FunnyPillsSpawnSystem : EntitySystem
{
    [Dependency] private readonly StationSpawningSystem _spawn = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_proto.TryIndex<StartingGearPrototype>("FunnyPillsStartingGear", out var gear))
            return;

        _spawn.EquipStartingGear(ev.Mob, gear);
    }
}
