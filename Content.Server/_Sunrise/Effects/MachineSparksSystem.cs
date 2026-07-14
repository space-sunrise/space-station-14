using Content.Server.Destructible;
using Content.Shared._Sunrise.Effects;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Effects;

public sealed class MachineSparksSystem : EntitySystem
{
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MachineSparksComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMachineSparksComponent, MachineSparksComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var active, out var machineSparks, out var xform))
        {
            if (_timing.CurTime < active.NextSparkTime)
                continue;

            if (machineSparks.LowHealthEffects.Count == 0)
                continue;

            SpawnSparkEffect(xform, _random.Pick(machineSparks.LowHealthEffects));
            active.NextSparkTime = GetNextSparkTime(machineSparks);
        }
    }

    private void OnDamageChanged(Entity<MachineSparksComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased &&
            ent.Comp.ImpactEffects.Count > 0 &&
            _random.Prob(ent.Comp.ImpactEffectProbability))
        {
            SpawnSparkEffect(Transform(ent), _random.Pick(ent.Comp.ImpactEffects));
        }

        if (IsLowHealth(ent))
        {
            if (!HasComp<ActiveMachineSparksComponent>(ent))
            {
                var active = EnsureComp<ActiveMachineSparksComponent>(ent);
                active.NextSparkTime = GetNextSparkTime(ent.Comp);
            }
        }
        else
        {
            RemComp<ActiveMachineSparksComponent>(ent);
        }
    }

    private bool IsLowHealth(Entity<MachineSparksComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable))
            return false;

        if (!TryComp<DestructibleComponent>(ent, out var destructible))
            return false;

        var destroyedAt = _destructible.DestroyedAt(ent, destructible);
        if (destroyedAt == FixedPoint2.MaxValue || destroyedAt <= FixedPoint2.Zero)
            return false;

        return damageable.TotalDamage.Float() >= destroyedAt.Float() * ent.Comp.LowHealthDamageFraction;
    }

    private TimeSpan GetNextSparkTime(MachineSparksComponent component)
    {
        var min = component.MinLowHealthSparkDelay.TotalSeconds;
        var max = Math.Max(min, component.MaxLowHealthSparkDelay.TotalSeconds);
        var delay = min + (max - min) * _random.NextDouble();
        return _timing.CurTime + TimeSpan.FromSeconds(delay);
    }

    private void SpawnSparkEffect(TransformComponent xform, EntProtoId effect)
    {
        var coordinates = GetNetCoordinates(xform.Coordinates);
        var filter = Filter.Pvs(xform.Coordinates, entityMan: EntityManager);

        RaiseNetworkEvent(new ImpactEffectEvent(effect.Id, coordinates), filter);
    }
}

[RegisterComponent]
public sealed partial class ActiveMachineSparksComponent : Component
{
    /// <summary>
    /// The time when the next low-health spark effect may be spawned.
    /// </summary>
    public TimeSpan NextSparkTime;
}
