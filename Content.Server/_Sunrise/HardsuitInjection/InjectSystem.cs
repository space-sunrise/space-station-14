using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Administration.Logs;
using Robust.Shared.Audio.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Timing;
using Content.Shared._Sunrise.HardsuitInjection.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared._Sunrise.HardsuitInjection.EntitySystems;
using Content.Shared.Interaction;

namespace Content.Server._Sunrise.HardsuitInjection.EntitySystems;

public sealed partial class InjectSystem : SharedInjectSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStrippableSystem _strippable = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _sharedAdminLogSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectComponent, UpdateECEvent>(OnUpdateEC);
        SubscribeLocalEvent<InjectComponent, InteractUsingEvent>(OnHardsuitInteractUsing, before: new[] { typeof(ItemSlotsSystem) });

        InitializeBaseEvents();
        InitializeActionEvents();
        InitializeDoAfterEvents();
    }

    #region Own Events

    private void OnUpdateEC(EntityUid uid, InjectComponent component, UpdateECEvent args)
    {
        var beakerUid = GetEntity(args.BeakerUid);

        if (!TryComp<SolutionContainerManagerComponent>(beakerUid, out var solutionContainerComponent)) return;
        if (!_solutions.TryGetSolution((beakerUid, solutionContainerComponent), "beaker", out var solutionEntity, out var _)) return;

        var removedSolution = _solutions.SplitSolution(solutionEntity.Value, args.ReagentTransfer.Value);
        args.RemovedReagentAmount = removedSolution;
    }

    private void OnHardsuitInteractUsing(EntityUid uid, InjectComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;
        
        var used = args.Used;
        var user = args.User;

        if (!TryComp<AmpulaComponent>(used, out var ampulaComponent)) 
        {
            return;
        }
        args.Handled = true;

        if (_netManager.IsClient) return;

        if (!TryComp<ItemSlotsComponent>(uid, out var itemslots)) return;
        if (!_itemSlotsSystem.TryGetSlot(uid, component.ContainerId, out var itemslot, itemslots)) return;
        if (!itemslot.InsertOnInteract) return;

        if (itemslot.Locked)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuitinjection-closed"), user, user);
            return;
        }

        if (!_itemSlotsSystem.CanInsert(uid, used, user, itemslot, swap: itemslot.Swap)) return;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.AmpulaInsertDelay, new AmpulaInsertDoAfterEvent(), uid, target: args.Target, used: used)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = true,
            BlockDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            return;
        }
    }


    #endregion
}
