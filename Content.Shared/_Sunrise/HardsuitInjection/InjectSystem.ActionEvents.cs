

using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared._Sunrise.HardsuitInjection.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.HardsuitInjection.EntitySystems;

public sealed partial class InjectSystem
{
    private void InitializeActionEvents()
    {
        SubscribeLocalEvent<InjectComponent, ToggleECEvent>(OnToggleEC);
        SubscribeLocalEvent<InjectComponent, InjectionEvent>(OnInjection);
        SubscribeLocalEvent<InjectComponent, GetItemActionsEvent>(OnGetItemAction);
    }

    private void OnGetItemAction(EntityUid uid, InjectComponent component, GetItemActionsEvent args)
    {
        if (!_timing.IsFirstTimePredicted) return;
        if ((args.SlotFlags | component.RequiredFlags) != component.RequiredFlags) return;

        args.AddAction(ref component.ToggleInjectionActionEntity, component.ToggleInjectionAction);
        args.AddAction(ref component.InjectionActionEntity, component.InjectionAction);
    }

    private void OnToggleEC(EntityUid uid, InjectComponent component, ToggleECEvent args)
    {
        if (args.Handled) return;
        if (_netManager.IsClient) return;

        if (!component.CanBeOpened)
        {
            args.Handled = true;
            return;
        }

        if (component.AlwaysOpen)
        {
            component.Locked = false;
            args.Handled = true;
            component.ToggleInjectionActionEntity = args.Action;
            return;
        }

        if (component.OpenCloseDelay > TimeSpan.Zero)
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Performer, component.OpenCloseDelay, new ToggleSlotDoAfterEvent(), uid)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                DistanceThreshold = 2,
            });
            args.Handled = true;
            component.ToggleInjectionActionEntity = args.Action;
            return;
        }

        component.Locked = false;
        args.Handled = true;
        component.ToggleInjectionActionEntity = args.Action;

    }

    private void OnInjection(EntityUid uid, InjectComponent component, InjectionEvent args)
    {
        if (args.Handled) return;
        if (_netManager.IsClient) return;

        args.Handled = true;
        component.InjectionActionEntity = args.Action;

        Inject(uid, uid);
    }
}
