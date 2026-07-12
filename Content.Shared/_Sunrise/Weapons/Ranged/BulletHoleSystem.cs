using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Shared._Sunrise.Weapons.Ranged;

public sealed class BulletHoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private const int MaxCount = 24;
    private const string BulletHoleState = "bullethole";

    public override void Initialize()
    {
        base.Initialize();

        if (!_net.IsServer)
            return;

        SubscribeLocalEvent<BulletHoleGeneratorComponent, ProjectileDamageDealtEvent>(OnProjectileDamageDealt);
        SubscribeLocalEvent<BulletHoleGeneratorComponent, HitscanDamageDealtEvent>(OnHitscanDamageDealt);
    }

    private void OnProjectileDamageDealt(Entity<BulletHoleGeneratorComponent> ent, ref ProjectileDamageDealtEvent args)
    {
        if (!CanCreateBulletHole(ent.Comp, args.DamageDealt))
            return;

        TryApplyBulletHole(args.Target, args.HitPosition, args.Direction, ent.Comp);
    }

    private void OnHitscanDamageDealt(Entity<BulletHoleGeneratorComponent> ent, ref HitscanDamageDealtEvent args)
    {
        if (!CanCreateBulletHole(ent.Comp, args.DamageDealt))
            return;

        if (args.HitPosition is not { } hitPosition)
            return;

        TryApplyBulletHole(args.Target, hitPosition, args.Direction, ent.Comp);
    }

    private void TryApplyBulletHole(
        EntityUid target,
        Vector2 hitPosition,
        Vector2 direction,
        BulletHoleGeneratorComponent generator)
    {
        if (_whitelist.IsWhitelistFail(generator.TargetWhitelist, target))
            return;

        if (!TryComp(target, out TransformComponent? transform) || direction.IsLengthZero())
            return;

        var appearance = EnsureComp<AppearanceComponent>(target);
        var bulletHole = EnsureComp<BulletHoleComponent>(target);

        if (bulletHole.Holes.Count >= MaxCount)
            return;

        var hitCoordinates = _transform.ToCoordinates((target, transform), new MapCoordinates(hitPosition, transform.MapID));
        var rotation = direction.ToWorldAngle() - _transform.GetWorldRotation(transform);
        bulletHole.Holes.Add(new BulletHoleVisualData(BulletHoleState, hitCoordinates.Position, rotation));
        _appearance.SetData(target, BulletHoleVisuals.Holes, bulletHole.Holes.ToArray(), appearance);
    }

    private static bool CanCreateBulletHole(BulletHoleGeneratorComponent generator, DamageSpecifier damage)
    {
        return damage.DamageDict.TryGetValue(generator.RequiredDamageType, out var value) && value > 0;
    }
}
