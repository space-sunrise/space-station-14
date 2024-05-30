using Content.Server.GameTicking.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Events;
using Robust.Shared.Map;
using Robust.Shared.Random;
using LateJoinSpawnRuleComponent = Content.Server._Sunrise.StationEvents.Components.LateJoinSpawnRuleComponent;

namespace Content.Server._Sunrise.StationEvents.Events;

public sealed class LateJoinSpawnRule : StationEventSystem<LateJoinSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, LateJoinSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        var locations = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        while (locations.MoveNext(out _, out var spawnPoint, out var transform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.LateJoin)
                continue;

            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station != station)
                continue;

            validLocations.Add(transform.Coordinates);
        }

        if (validLocations.Count == 0)
            return;

        var spawn = RobustRandom.Pick(validLocations);

        Spawn(comp.Prototype, spawn);
    }
}
