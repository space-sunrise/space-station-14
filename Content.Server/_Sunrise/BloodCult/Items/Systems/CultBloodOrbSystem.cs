using Content.Server.Hands.Systems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

public sealed class CultBloodOrbSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<CultBloodOrbComponent, ThrowDoHitEvent>(OnThrowDoHit);
    }

    private void OnThrowDoHit(EntityUid uid, CultBloodOrbComponent component, ThrowDoHitEvent args)
    {
        if (HasComp<ConstructComponent>(args.Target))
            return;

        if (HasComp<BloodCultistComponent>(args.Target))
            _handsSystem.TryPickup(args.Target, uid, checkActionBlocker: false);
        else
        {
            if (HasComp<MobStateComponent>(args.Target))
            {
                _damageableSystem.TryChangeDamage(args.Target,
                    component.DamagePerBlood * component.BloodCharges,
                    origin: uid);
                _audio.PlayPvs(component.BreakSound, uid);
                QueueDel(uid);
            }
        }
    }
}
