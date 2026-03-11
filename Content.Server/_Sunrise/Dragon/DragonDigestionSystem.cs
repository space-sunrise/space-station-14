using Content.Server._Sunrise.Dragon.Components;
using Content.Server.Body.Systems;
using Content.Server.Dragon;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Organ;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Sunrise.Dragon;

public sealed class DragonDigestionSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonDevourMobEvent>(OnDragonDevourMob);
        SubscribeLocalEvent<DragonDigestingComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DragonDigestingComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!_container.TryGetContainingContainer((uid, null, null), out var container) ||
                container.Owner != component.Devourer ||
                container.ID != DevourerComponent.StomachContainerId)
            {
                RemComp<DragonDigestingComponent>(uid);
                continue;
            }

            if (_timing.CurTime >= component.DigestsAt)
            {
                FinishDigestion(uid);
                continue;
            }

            if (_timing.CurTime < component.NextDamageAt)
                continue;

            component.NextDamageAt = _timing.CurTime + TimeSpan.FromSeconds(component.DigestionDamageInterval);

            if (!_mobState.IsDead(uid))
                _damageable.TryChangeDamage(uid, component.DigestionDamage, ignoreResistances: true);
        }
    }

    private void OnDragonDevourMob(DragonDevourMobEvent args)
    {
        if (!TryComp<DragonComponent>(args.Devourer, out var dragon))
            return;

        _damageable.TryChangeDamage(args.Devoured.Owner, dragon.DamageOnDevour, ignoreResistances: true);

        var interval = MathF.Max(dragon.DigestionDamageInterval, 0.1f);
        var digested = EnsureComp<DragonDigestingComponent>(args.Devoured.Owner);
        digested.Devourer = args.Devourer;
        digested.DigestionDamage = dragon.DigestionDamage;
        digested.DigestionDamageInterval = interval;
        digested.NextDamageAt = _timing.CurTime + TimeSpan.FromSeconds(interval);
        digested.DigestsAt = _timing.CurTime + TimeSpan.FromSeconds(MathF.Max(dragon.DigestionDuration, interval));
    }

    private void OnRemovedFromContainer(Entity<DragonDigestingComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.Owner != ent.Comp.Devourer || args.Container.ID != DevourerComponent.StomachContainerId)
            return;

        RemComp<DragonDigestingComponent>(ent);
    }

    private void FinishDigestion(EntityUid uid)
    {
        RemCompDeferred<DragonDigestingComponent>(uid);

        if (TryComp<BodyComponent>(uid, out var body))
        {
            _body.GibBody(uid, gibOrgans: true, body: body, launchGibs: false);
            return;
        }

        QueueDel(uid);
    }
}
