using Content.Shared.Projectiles;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.Weapons.Ranged;

public sealed class BulletHoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const int MaxState = 10;
    private const int MaxCount = 24;

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
        if (args.DamageDealt.Empty)
            return;

        TryApplyBulletHole(args.Target);
    }

    private void OnHitscanDamageDealt(Entity<BulletHoleGeneratorComponent> ent, ref HitscanDamageDealtEvent args)
    {
        if (args.DamageDealt.Empty)
            return;

        TryApplyBulletHole(args.Target);
    }

    private void TryApplyBulletHole(EntityUid target)
    {
        if (!TryComp<BulletHoleComponent>(target, out var bulletHole))
            return;

        if (!TryComp<AppearanceComponent>(target, out var appearance))
            return;

        bulletHole.Count++;

        if (bulletHole.State < 1 || bulletHole.State > MaxState)
            bulletHole.State = _random.Next(1, MaxState + 1);

        var count = Math.Min(bulletHole.Count, MaxCount);
        var state = $"bhole_{bulletHole.State}_{count}";
        _appearance.SetData(target, BulletHoleVisuals.State, state, appearance);
    }
}
