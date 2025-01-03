using System.Linq;
using Content.Server._Sunrise.BloodCult.UI;
using Content.Server.Body.Components;
using Content.Shared._Sunrise.BloodCult.Actions;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.UI;
using Content.Shared.Actions;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stacks;
using Content.Shared.StatusEffect;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.BloodCult.Runes.Systems
{
    public partial class BloodCultSystem
    {
        private void InitializeActions()
        {
            SubscribeLocalEvent<BloodCultistComponent, CultTwistedConstructionActionEvent>(OnTwistedConstructionAction);
            SubscribeLocalEvent<BloodCultistComponent, CultSummonDaggerActionEvent>(OnSummonDaggerAction);
            SubscribeLocalEvent<BloodCultistComponent, CultEmpPulseTargetActionEvent>(OnElectromagneticPulse);
            SubscribeLocalEvent<BloodCultistComponent, CultSummonCombatEquipmentTargetActionEvent>(OnSummonCombatEquipment);
            SubscribeLocalEvent<BloodCultistComponent, CultConcealPresenceWorldActionEvent>(OnConcealPresence);
            SubscribeLocalEvent<BloodCultistComponent, CultTeleportTargetActionEvent>(OnTeleport);
            SubscribeLocalEvent<BloodCultistComponent, CultStunTargetActionEvent>(OnStunTarget);
            SubscribeLocalEvent<BloodCultistComponent, CultShadowShacklesTargetActionEvent>(OnShadowShackles);
            SubscribeLocalEvent<BloodCultistComponent, ShadowShacklesDoAfterEvent>(OnShadowShacklesDoAfter);
            SubscribeLocalEvent<BloodCultistComponent, TeleportSpellUserEvent>(OnTeleportSpellUser);
            SubscribeLocalEvent<BloodCultistComponent, CultBloodRitualInstantActionEvent>(OnCultBloodRitual);
        }

        private void OnCultBloodRitual(EntityUid uid, BloodCultistComponent component, CultBloodRitualInstantActionEvent args)
        {
            var hands = _handsSystem.EnumerateHands(uid);
            var enumerateHands = hands as Hand[] ?? Enumerable.ToArray<Hand>(hands);
            foreach (var enumerateHand in enumerateHands)
            {
                if (enumerateHand.Container == null)
                    continue;
                foreach (var containerContainedEntity in enumerateHand.Container.ContainedEntities)
                {
                    if (!TryComp(containerContainedEntity, out MetaDataComponent? metaData))
                        continue;
                    if (metaData.EntityPrototype == null)
                        continue;
                    if (metaData.EntityPrototype.ID == BloodCultSystem.CultBloodSpeelPrototypeId)
                    {
                        QueueDel(containerContainedEntity);
                        return;
                    }
                    _handsSystem.TryDrop(uid, checkActionBlocker: false);
                }
            }

            var spell = Spawn(BloodCultSystem.CultBloodSpeelPrototypeId, Transform(uid).Coordinates);
            var isPickup = _handsSystem.TryPickup(uid, spell, checkActionBlocker: false,
                animateUser: false, animate: false);
            if (!isPickup)
                QueueDel(spell);
        }

        private void OnTeleportSpellUser(EntityUid uid, BloodCultistComponent component, TeleportSpellUserEvent args)
        {
            if (!_entityManager.TryGetComponent<ActionsComponent>(uid, out var actionsComponent))
                return;
            foreach (var userAction in actionsComponent.Actions)
            {
                var entityPrototypeId = MetaData(userAction).EntityPrototype?.ID;
                if (entityPrototypeId == "ActionCultTeleport")
                    _actionsSystem.RemoveAction(uid, userAction, actionsComponent);
            }
        }

        private void OnShadowShacklesDoAfter(EntityUid uid, BloodCultistComponent component,
            ShadowShacklesDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            if (args.Args.Target == null)
                return;

            var cuffs = Spawn("CultistCuffs", Transform(uid).Coordinates);
            if (TryComp<HandcuffComponent>(cuffs, out var handcuffComponent))
            {
                _audio.PlayPvs(handcuffComponent.EndCuffSound, cuffs);
                _cuffable.TryAddNewCuffs(args.Args.Target.Value, args.Args.User, cuffs);
            }
        }

        private void OnStunTarget(EntityUid uid, BloodCultistComponent component, CultStunTargetActionEvent args)
        {
            if (!HasComp<StatusEffectsComponent>(args.Target))
                return;

            if (HasComp<BorgChassisComponent>(args.Target))
                _empSystem.EmpPulse(_transformSystem.GetMapCoordinates(args.Target), 2, 100000, 5f);

            _stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(6), true);
            _stuttering.DoStutter(args.Target, TimeSpan.FromSeconds(30), true);
            _flashSystem.Flash(args.Target, uid, null, 3, 10);

            if (TryComp<BloodstreamComponent>(uid, out var bloodstreamComponent))
                _bloodstreamSystem.TryModifyBloodLevel(uid, -10, bloodstreamComponent);

            args.Handled = true;
        }

        private void OnTeleport(EntityUid uid, BloodCultistComponent component, CultTeleportTargetActionEvent args)
        {
            if (!TryComp<BloodstreamComponent>(args.Performer, out _) || !TryComp<ActorComponent>(uid, out var actor))
                return;

            var eui = new TeleportSpellEui(args.Performer, args.Target);
            _euiManager.OpenEui(eui, actor.PlayerSession);
            eui.StateDirty();

            args.Handled = true;
        }

        private void OnConcealPresence(EntityUid uid, BloodCultistComponent component, CultConcealPresenceWorldActionEvent args)
        {
            if (!TryComp<BloodstreamComponent>(args.Performer, out _))
                return;
        }

        private void OnSummonCombatEquipment(
            EntityUid uid,
            BloodCultistComponent component,
            CultSummonCombatEquipmentTargetActionEvent args)
        {
            if (!TryComp<BloodstreamComponent>(args.Performer, out _))
                return;

            _bloodstreamSystem.TryModifyBloodLevel(uid, -20);

            var coordinates = Transform(uid).Coordinates;
            var helmet = Spawn("ClothingHeadHelmetCult", coordinates);
            var armor = Spawn("ClothingOuterArmorCult", coordinates);
            var shoes = Spawn("ClothingShoesCult", coordinates);
            var blade = Spawn("EldritchBlade", coordinates);
            var bola = Spawn("CultBola", coordinates);

            _inventorySystem.TryUnequip(uid, "head");
            _inventorySystem.TryUnequip(uid, "outerClothing");
            _inventorySystem.TryUnequip(uid, "shoes");

            _inventorySystem.TryEquip(uid, helmet, "head", force: true);
            _inventorySystem.TryEquip(uid, armor, "outerClothing", force: true);
            _inventorySystem.TryEquip(uid, shoes, "shoes", force: true);

            _handsSystem.PickupOrDrop(uid, blade);
            _handsSystem.PickupOrDrop(uid, bola);

            args.Handled = true;
        }

        private void OnElectromagneticPulse(EntityUid uid, BloodCultistComponent component, CultEmpPulseTargetActionEvent args)
        {
            _empSystem.EmpPulse(_transformSystem.GetMapCoordinates(uid), 5, 10000, 5f);
            _empSystem.EmpPulse(_transformSystem.GetMapCoordinates(uid), 2, 100000, 10f);

            args.Handled = true;
        }

        private void OnShadowShackles(EntityUid uid, BloodCultistComponent component, CultShadowShacklesTargetActionEvent args)
        {
            if (!TryComp<BloodstreamComponent>(args.Performer, out _))
                return;

            if (!_statusEffectsSystem.HasStatusEffect(args.Target, "Stun"))
            {
                _popupSystem.PopupEntity("Цель не оглушена", uid, uid);
                return;
            }

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager ,uid, 1f,
                new ShadowShacklesDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            });

            args.Handled = true;
        }

        private void OnTwistedConstructionAction(
            EntityUid uid,
            BloodCultistComponent component,
            CultTwistedConstructionActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstreamComponent))
                return;

            if (!_entityManager.TryGetComponent<StackComponent>(args.Target, out var stack))
                return;

            if (stack.StackTypeId != BloodCultSystem.SteelPrototypeId)
                return;

            var transform = Transform(args.Target).Coordinates;
            var count = stack.Count;

            _entityManager.DeleteEntity(args.Target);

            var material = _entityManager.SpawnEntity(BloodCultSystem.RunicMetalPrototypeId, transform);

            _bloodstreamSystem.TryModifyBloodLevel(args.Performer, -15, bloodstreamComponent);

            if (!_entityManager.TryGetComponent<StackComponent>(material, out var stackNew))
                return;

            stackNew.Count = count;

            _popupSystem.PopupEntity(Loc.GetString("Конвертируем сталь в руиник металл!"), args.Performer, args.Performer);
            args.Handled = true;
        }

        private void OnSummonDaggerAction(EntityUid uid, BloodCultistComponent component, CultSummonDaggerActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<BloodstreamComponent>(args.Performer, out var bloodstreamComponent))
                return;

            var xform = Transform(args.Performer).Coordinates;
            var dagger = _entityManager.SpawnEntity(BloodCultSystem.RitualDaggerPrototypeId, xform);

            _bloodstreamSystem.TryModifyBloodLevel(args.Performer, -30, bloodstreamComponent);
            _handsSystem.TryPickupAnyHand(args.Performer, dagger);
            args.Handled = true;
        }
    }
}
