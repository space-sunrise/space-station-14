﻿using Content.Shared._RMC14.Explosion;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Explosion;

public sealed class SharedSunriseExplosionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExplosiveComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<ExplosiveComponent> ent, ref ComponentInit args)
    {
        TryAddExplosionEffect(ent, ent.Comp.ExplosionType);
    }

    public bool TryAddExplosionEffect(EntityUid uid, string explosionType)
    {
        if (!_prototype.TryIndex<ExplosionPrototype>(explosionType, out var explosionPrototype))
            return false;

        if (explosionPrototype.EffectType != ExplosionEffectType.Fancy)
            return false;

        EnsureComp<CMExplosionEffectComponent>(uid);
        return true;
    }
}
