﻿using Content.Shared._Sunrise.Boss.Components;
using Content.Shared._Sunrise.Boss.Events;
using Content.Shared._Sunrise.Boss.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Boss.Systems;

/// <summary>
/// Система для тентаклей
/// </summary>
public sealed class HellSpawnTentacleSystem : SharedHellSpawnTentacleSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HellSpawnTentacleComponent, HellSpawnTentacleActionEvent>(OnTentacleActionEvent);
    }

    private void OnTentacleActionEvent(EntityUid uid,
        HellSpawnTentacleComponent component,
        HellSpawnTentacleActionEvent args)
    {
        if (args.Handled || args.Performer != uid)
            return;

        var coords = args.Coords;

        if (coords != null)
        {
            SpawnTentacle(coords.Value, args.Left ? GrabLeftEntityId : GrabRightEntityId);
            args.Handled = true;
            return;
        }

        if (args.Entity != null)
        {
            var entCoords = Transform(args.Entity.Value).Coordinates;
            SpawnTentacle(entCoords, args.Left ? GrabLeftEntityId : GrabRightEntityId);
            args.Handled = true;
            return;
        }
    }

    /// <summary>
    /// Это оберточная функция нужна на будущее
    /// </summary>
    /// <param name="coordinates"></param>
    /// <param name="protoId"></param>
    public void SpawnTentacle(EntityCoordinates coordinates, EntProtoId protoId)
    {
        Spawn(protoId, coordinates);
    }
}
