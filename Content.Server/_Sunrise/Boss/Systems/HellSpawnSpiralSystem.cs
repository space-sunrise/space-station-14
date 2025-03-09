using System.Numerics;
using System.Threading.Tasks;
using Content.Server._Sunrise.Boss.Components;
using Content.Server.Stunnable;
using Content.Shared._Sunrise.Boss.Components;
using Content.Shared._Sunrise.Boss.Events;
using Content.Shared.Actions;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.Boss.Systems;

public sealed class HellSpawnSpiralSystem : EntitySystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    [DataField]
    public string BulletProto = "BulletSkyFlare";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HellSpawnSpiralComponent, HellSpawnSpiralActionEvent>(OnSpiral);
        SubscribeLocalEvent<HellSpawnSpiralComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, HellSpawnSpiralComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, ref component.SpiralActionEntity, component.SpiralAction);
    }

    private async void OnSpiral(EntityUid uid, HellSpawnSpiralComponent component, HellSpawnSpiralActionEvent args)
    {
        var xform = Transform(uid);
        _stun.TrySlowdown(uid, TimeSpan.FromSeconds(2), true, 0f, 0f);

        var vectors = GenerateVectors(component.FireballCount);
        Console.WriteLine($"{vectors}");
        foreach (var vector in vectors)
        {
            Console.WriteLine($"{vector.X} {vector.Y}");
            SpawnSpiral(xform.Coordinates, vector);
        }
    }

    private List<Vector2> GenerateVectors(int N)
    {
        var vectors = new List<Vector2>();
        var angleStep = 2 * Math.PI / N;

        for (var i = 0; i < N; i++)
        {
            var angle = i * angleStep;
            var x = (float)Math.Cos(angle);
            var y = (float)Math.Sin(angle);
            vectors.Add(new Vector2(x, y));
        }
        return vectors;
    }

    public void SpawnSpiral(EntityCoordinates origin, Vector2 offset)
    {
        var bullet = SpawnAtPosition(BulletProto,
            new EntityCoordinates(origin.EntityId, origin.Position));
        var spiralComp = EnsureComp<SpiralMovementComponent>(bullet);
        spiralComp.TimeOffset = TimeSpan.FromSeconds(1);
        spiralComp.Offset = (float)Math.Acos(offset.X);

        if (offset.Y < 0)
        {
            spiralComp.Offset = -spiralComp.Offset;
        }
    }
}
