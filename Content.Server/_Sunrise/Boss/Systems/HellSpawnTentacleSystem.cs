using Content.Shared._Sunrise.Boss.Components;
using Content.Shared._Sunrise.Boss.Events;
using Content.Shared._Sunrise.Boss.Systems;
using Content.Shared.Actions;
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
        ref HellSpawnTentacleActionEvent args)
    {
        if (args.Handled || args.Performer != uid)
            return;

        var coords = args.Coords;

        if (coords != null)
        {
            SpawnTentacle(coords.Value, args.Left ? GrabLeftEntityId : GrabRightEntityId);
            return;
        }

        if (args.Entity != null)
        {
            var entCoords = Transform(args.Entity.Value).Coordinates;
            SpawnTentacle(entCoords, args.Left ? GrabLeftEntityId : GrabRightEntityId);
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
