using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server._Sunrise.EnergyShield;
using Content.Shared.Damage;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Examine;
using Content.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;

namespace Content.Server._Sunrise.EnergyShield;

public sealed class EnergyShieldSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EnergyShieldComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<EnergyShieldComponent, ItemToggleActivateAttemptEvent>(OnToggleAttempt);
    }

    private void OnDamage(Entity<EnergyShieldComponent> ent, ref DamageChangedEvent args)
    {
        if (!_itemToggle.IsActivated(ent.Owner))
            return;

        if (args.DamageDelta == null)
            return;

        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        var totalDamage = args.DamageDelta.GetTotal();
        if (totalDamage <= 0)
            return;

        var cost = totalDamage.Float() * ent.Comp.EnergyCostPerDamage;
        _battery.UseCharge(ent, cost, battery);
        _audio.PlayPvs(ent.Comp.AbsorbSound, ent);

        if (battery.CurrentCharge <= 0)
        {
            _itemToggle.Toggle(ent.Owner);
            _audio.PlayPvs(ent.Comp.ShutdownSound, ent);
        }
    }

    private void OnToggleAttempt(Entity<EnergyShieldComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (TryComp<BatteryComponent>(ent, out var battery) &&
            battery.CurrentCharge >= battery.MaxCharge * ent.Comp.MinChargeFractionForActivation)
        {
            return;
        }

        if (Exists(args.User))
        {
            _popup.PopupEntity(
                Loc.GetString("stunbaton-component-low-charge"),
                args.User.Value,
                args.User.Value,
                PopupType.Small
            );
        }

        args.Cancelled = true;
    }
}
