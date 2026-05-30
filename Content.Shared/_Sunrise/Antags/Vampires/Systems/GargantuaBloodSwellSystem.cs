using Content.Shared.Popups;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Content.Shared.Weapons.Ranged.Events;

using Content.Shared._Sunrise.Antags.Vampires.Components.Classes;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems;

public sealed class GargantuaBloodSwellSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GargantuaComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<GargantuaComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.User != ent.Owner
            || !_statusEffects.TryEffectsWithComp<ActiveBloodSwellComponent>(ent, out _))
            return;

        TryShowPopup(ent, args.Used);
        args.Cancel();
    }

    private void TryShowPopup(Entity<GargantuaComponent> ent, EntityUid used)
    {
        if (!_net.IsClient || !_timing.IsFirstTimePredicted)
            return;

        if (used == ent.Comp.BloodSwellShootLastGun
            && ent.Comp.BloodSwellShootNextPopupTime is { } next
            && !(_timing.CurTime > next))
            return;

        ent.Comp.BloodSwellShootLastGun = used;
        ent.Comp.BloodSwellShootNextPopupTime = _timing.CurTime + ent.Comp.BloodSwellShootPopupCooldown;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("vampire-blood-swell-cancel-shoot"), ent.Owner, ent.Owner);
    }
}
