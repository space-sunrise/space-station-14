using Content.Shared._Sunrise.Boss.Components;
using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Boss.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class SpiralMovementSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<SpiralMovementComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SpiralMovementComponent component, ComponentInit args)
    {
        if (component.OriginCoordinates is not null || component.SpawnTime is not null)
            return;

        var xform = Transform(uid);

        if (xform.Coordinates.EntityId == uid) // Это вызывается если превью в спавн меню было вызвано
            return;

        component.OriginCoordinates = xform.Coordinates;

        component.SpawnTime = _timing.CurTime - component.TimeOffset;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<SpiralMovementComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spiralComp, out var xform))
        {
            if (spiralComp.OriginCoordinates is null || spiralComp.SpawnTime is null)
                continue;
            if (xform.Coordinates.EntityId == uid) // Это вызывается если превью в спавн меню было вызвано
                continue;
            var deltaTime = (float)((_timing.CurTime - spiralComp.SpawnTime) / spiralComp.RadiusCoefficient);

            var x = spiralComp.OriginCoordinates.Value.X + deltaTime * (float)Math.Cos(spiralComp.OmegaCoefficient * deltaTime + spiralComp.Offset);
            var y = spiralComp.OriginCoordinates.Value.Y + deltaTime * (float)Math.Sin(spiralComp.OmegaCoefficient * deltaTime + spiralComp.Offset);

            _transform.SetCoordinates(uid, new EntityCoordinates(spiralComp.OriginCoordinates.Value.EntityId, x, y));
            _broadphase.RegenerateContacts(uid);
        }
    }
}
