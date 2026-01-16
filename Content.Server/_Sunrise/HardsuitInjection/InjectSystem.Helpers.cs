using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared._Sunrise.HardsuitInjection.Components;
using Content.Shared.Popups;
using System.Threading;
using Content.Server.Temperature.Components;
using Content.Server.Atmos.Components;
using Content.Shared._Sunrise.HardsuitInjection.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Cabinet;
using Content.Shared.DoAfter;

namespace Content.Server._Sunrise.HardsuitInjection.EntitySystems;

public sealed partial class InjectSystem : SharedInjectSystem
{

    //private void InitializeDoAfterEvent()
    //{
        //SubscribeLocalEvent<InjectComponent, InjectECDoAfterEvent>(OnDoAfterCloseComplete);
    //}

    /// <summary>
    /// Toggle EC on hardsuit
    /// </summary>
    /// <param name="uid">Hardsuit uid</param>
    /// <param name="user">The person who will be shown messages about the opening and closing of the EС</param>
    public void ToggleEC(EntityUid uid, EntityUid user)
    {
        if (!TryComp<InjectComponent>(uid, out var component)) return;
        if (!TryComp<ItemSlotsComponent>(uid, out var comp)) return;
        if (!TryComp<TemperatureProtectionComponent>(uid, out var tempProt)) return;
        if (!TryComp<PressureProtectionComponent>(uid, out var pressProt)) return;

        if (component.Container == null) return;

        component.Locked = !component.Locked;

        _itemSlotsSystem.SetLock(uid, component.ContainerId, component.Locked, comp);

        if (component.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-close"), user, user, PopupType.Medium);
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} closed EC of {_entManager.ToPrettyString(uid):wearer}");

            pressProt.HighPressureMultiplier = component.HighPressureMultiplier;
            pressProt.LowPressureMultiplier = component.LowPressureMultiplier;
            tempProt.HeatingCoefficient = component.HeatingCoefficient;
            tempProt.CoolingCoefficient = component.CoolingCoefficient;

            component.HighPressureMultiplier = 1;
            component.LowPressureMultiplier = 1;
            component.HeatingCoefficient = 1;
            component.CoolingCoefficient = 1;
        }
        else
        {
         _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-open"), user, user, PopupType.Medium);
        _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} opened EC of {_entManager.ToPrettyString(uid):wearer}");
        
        component.HighPressureMultiplier = pressProt.HighPressureMultiplier;
        component.LowPressureMultiplier = pressProt.LowPressureMultiplier;
        component.HeatingCoefficient = tempProt.HeatingCoefficient;
        component.CoolingCoefficient = tempProt.CoolingCoefficient;

        pressProt.HighPressureMultiplier = 1;
        pressProt.LowPressureMultiplier = 1;
        tempProt.HeatingCoefficient = 1;
        tempProt.CoolingCoefficient = 1;
  
        }

        Dirty(uid, component);
        Dirty(uid, tempProt);
        Dirty(uid, pressProt);

        if (!TryComp<InventoryComponent>(user, out var invComp)) return;

        if (!_inventorySystem.TryGetSlot(uid, "outerClothing", out var uHardsuit, inventory: invComp))
            return;

        var gotEquippedEvent = new GotEquippedEvent(user, uid, uHardsuit);
        RaiseLocalEvent(uid, gotEquippedEvent, true);

        if (component.AutoClose && !component.Locked)
            StartAutoClose(uid, user, component);  
    }

    private void StartAutoClose(EntityUid uid, EntityUid user, InjectComponent component)
    {
        // Отменяем предыдущий таймер, если был
        component.AutoCloseCancelToken?.Cancel();
        component.AutoCloseCancelToken = new CancellationTokenSource();

        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.AutoCloseDelay, new InjectECDoAfterEvent(), uid, Transform(uid).ParentUid, uid)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            Broadcast = false,
            NeedHand = true,
            BlockDuplicate = true,
            Hidden = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs)) return;
    }

    /// <summary>
    /// Inject reagent from ampula from hardsuit
    /// </summary>
    /// <param name="uid">Hardsuit uid</param>
    /// <param name="performer">Initiator of injection (For admin log)</param>
    public void Inject(EntityUid uid, EntityUid performer)
    {
        if (!TryComp<InjectComponent>(uid, out var component)) return;
        var action = _actionsSystem.GetAction(component!.InjectionActionEntity);

        if (action == null) return;
        if (action.Value.Comp.AttachedEntity == null) return;
        if (TryComp<ItemSlotsComponent>(action.Value.Comp.AttachedEntity, out var itemslots)) return;

        var user = action.Value.Comp.AttachedEntity.Value;
        var beaker = _itemSlotsSystem.GetItemOrNull(uid, component.ContainerId, itemslots);

        if (beaker == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-nobeaker"), user, user);

            return;
        }

        var actualBeaker = beaker.Value;

        if (!_solutions.TryGetSolution(actualBeaker, "beaker", out var solution)) return;
        if (!_solutions.TryGetInjectableSolution(
            (user, Comp<InjectableSolutionComponent>(user), Comp<SolutionContainerManagerComponent>(user)),
            out var targetSolutionEntity,
            out var targetSolution
        )) return;

        if (solution.Value.Comp.Solution.Volume <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-empty"), user, user);

            return;
        }

        var transferAmount = FixedPoint2.Min(solution.Value.Comp.Solution.Volume, targetSolution.AvailableVolume);
        if (transferAmount <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-full"), user, user);

            return;
        }

        var ev = new UpdateECEvent(GetNetEntity(actualBeaker), solution.Value.Comp.Solution, transferAmount);
        RaiseLocalEvent(uid, ev);

        if (ev.RemovedReagentAmount == null) return;

        var removedSolution = ev.RemovedReagentAmount;
        if (!targetSolution.CanAddSolution(removedSolution)) return;

        if (performer == uid)
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} injected his ES into yourself with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution}");
        else
            _sharedAdminLogSystem.Add(LogType.ForceFeed, $"{_entManager.ToPrettyString(user):user} ES injected with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution}");

        _reactiveSystem.DoEntityReaction(user, removedSolution, ReactionMethod.Injection);
        _solutions.TryAddSolution(targetSolutionEntity.Value, removedSolution);

        _audio.PlayPvs(component.InjectSound, user);
        _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), user, user);
    }

}
