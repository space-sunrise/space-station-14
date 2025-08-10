using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Sunrise.Weapons.Ranged;

public sealed class ConsumableAmmoSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ConsumableAmmoComponent, InteractUsingEvent>(OnLoadAmmo);
        SubscribeLocalEvent<ConsumableAmmoComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<ConsumableAmmoComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<ConsumableAmmoComponent, ExaminedEvent>(OnExamine);
    }

    private void OnLoadAmmo(Entity<ConsumableAmmoComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<StackComponent>(args.Used, out var stack))
            return;

        var itemProtoId = Prototype(args.Used);
        if (itemProtoId == null || !ent.Comp.LoadableItems.TryGetValue(itemProtoId, out var multiplier))
            return;

        if (ent.Comp.CurrentCharges >= ent.Comp.MaxCharges)
        {
            _popup.PopupPredicted(Loc.GetString("consumable-ammo-fully-charged"), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        var chargesPerItem = (1.0f / ent.Comp.ItemsPerCharge) * multiplier; // сколько зарядов даёт один предмет с учётом множителя
        var chargesCanAdd = ent.Comp.MaxCharges - ent.Comp.CurrentCharges;  // сколько зарядов можно ещё добавить
        var itemsNeeded = (int)Math.Ceiling(chargesCanAdd / chargesPerItem); // сколько нужно для полного заряда
        var itemsToConsume = Math.Min(itemsNeeded, stack.Count); // округляем в большую сторону

        if (itemsToConsume == 0)
            return;

        var potentialChargesAdded = (int)Math.Floor(itemsToConsume * chargesPerItem); // потенциально добавленные заряды
        if (potentialChargesAdded <= 0)
            return;

        if (!_stack.Use(args.Used, itemsToConsume, stack))
            return;

        var actualChargesAdded = Math.Min(potentialChargesAdded, chargesCanAdd); // узнаём сколько реально добавить нужно
        ent.Comp.CurrentCharges += actualChargesAdded;

        // это чтоб не спамило "нет зарядов" при попытке стрелять без них
        if (ent.Comp.PopupShownOnEmpty)
        {
            ent.Comp.PopupShownOnEmpty = false;
            Dirty(ent);
        }

        if (ent.Comp.LoadSound != null)
            _audio.PlayPredicted(ent.Comp.LoadSound, ent.Owner, args.User);

        var message = Loc.GetString("consumable-ammo-charged", ("chargesAdded", actualChargesAdded), ("currentCharges", ent.Comp.CurrentCharges), ("maxCharges", ent.Comp.MaxCharges));
        _popup.PopupPredicted(message, ent.Owner, args.User);

        args.Handled = true;
        Dirty(ent);
    }


    private void OnShotAttempted(Entity<ConsumableAmmoComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.CurrentCharges < ent.Comp.ChargesPerShot)
        {
            args.Cancel();
            if (!ent.Comp.PopupShownOnEmpty)
            {
                ent.Comp.PopupShownOnEmpty = true;
                _popup.PopupPredicted(Loc.GetString("consumable-ammo-empty"), ent.Owner, args.User);

                if (ent.Comp.EmptySound != null)
                    _audio.PlayPredicted(ent.Comp.EmptySound, ent.Owner, args.User);

                Dirty(ent);
            }
            return;
        }

        if (ent.Comp.PopupShownOnEmpty)
        {
            ent.Comp.PopupShownOnEmpty = false;
            Dirty(ent);
        }
    }

    private void OnTakeAmmo(Entity<ConsumableAmmoComponent> ent, ref TakeAmmoEvent args)
    {
        if (ent.Comp.CurrentCharges < args.Shots * ent.Comp.ChargesPerShot)
        {
            args.Reason = Loc.GetString("consumable-ammo-empty");
            return;
        }

        ent.Comp.CurrentCharges -= args.Shots * ent.Comp.ChargesPerShot; // вычитаем использованные заряды
        for (var i = 0; i < args.Shots; i++)
        {
            var projectile = Spawn(ent.Comp.ProjectilePrototypeId, args.Coordinates);
            if (!TryComp<AmmoComponent>(projectile, out var ammoComp))
                continue;
            args.Ammo.Add((projectile, ammoComp));
        }
        Dirty(ent);
    }

    private void OnExamine(Entity<ConsumableAmmoComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("charges-count-text", ("chargesText", ent.Comp.CurrentCharges))); // отображение количества зарядов
    }
}
