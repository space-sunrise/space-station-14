using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.AoEHeal;

/// <summary>
/// Лечение по области
/// </summary>
public sealed class AoEHealSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private TimeSpan? _prev = TimeSpan.Zero;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(2);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime - _prev < _delay)
            return;
        _prev = _timing.CurTime;

        var query = EntityQueryEnumerator<TransformComponent, AoEHealComponent>();
        while (query.MoveNext(out _, out var xform, out var aoEHealComponent))
        {
            var targetsQuery =
                _lookupSystem.GetEntitiesInRange<DamageableComponent>(xform.Coordinates, aoEHealComponent.Range);
            foreach (var target in targetsQuery
                         // Проходит ли ВЛ?
                         .Where(target => !_whitelist.IsWhitelistFail(aoEHealComponent.EntityWhitelist, target))
                         // Если нам важно, то жива ли цель?
                         .Where(target => !aoEHealComponent.AliveTargets || _mobState.IsAlive(target)))
            {
                if (aoEHealComponent.Threshold != null && // AoE компоненту важно хилить до какого-то уровня от макс здоровья
                    _mobThreshold.TryGetDeadThreshold(target, out var threshold) &&
                    target.Comp.Damage.GetTotal() < threshold * (1f - aoEHealComponent.Threshold)) // Не лечим если урона мало
                    continue;

                _damageableSystem.TryChangeDamage(target, aoEHealComponent.Damage);
            }
        }
    }
}
