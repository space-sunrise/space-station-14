﻿using Content.Shared._Sunrise.Boss.Components;
using Content.Shared._Sunrise.Boss.Systems;
using Content.Shared.Damage;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Sunrise.Boss.Systems;

/// <inheritdoc/>
public sealed class DamageOnCollideSystem : SharedDamageOnCollideSystem
{

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _sawmill = _log.GetSawmill("damageoncollide");

        SubscribeLocalEvent<DamageOnCollideComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<DamageOnCollideComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<DamageOnCollideComponent, ThrowDoHitEvent>(OnThrow);
    }

    private void OnInit(EntityUid uid, DamageOnCollideComponent component, ref ComponentStartup args)
    {
        if (TryComp<PhysicsComponent>(uid, out var body))
            _broadphase.RegenerateContacts(uid, body);
        var query = _lookup.GetEntitiesInRange<DamageableComponent>(Transform(uid).Coordinates, 0.8f);
        foreach (var entity in query)
        {
            Damage(entity, component);
        }
    }

    private void OnStartCollide(EntityUid uid, DamageOnCollideComponent component, ref StartCollideEvent args)
    {
        Damage(args.OtherEntity, component);
    }

    private void OnThrow(EntityUid uid, DamageOnCollideComponent component, ThrowDoHitEvent args)
    {
        Damage(args.Target, component);
    }

    public void Damage(EntityUid uid, DamageOnCollideComponent component)
    {
        if (_whitelist.IsBlacklistPass(component.Blacklist, uid))
            return;
        _damageable.TryChangeDamage(uid, component.Damage);
    }
}
