using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Eye.NightVision.Systems;

public sealed class NightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        if(_net.IsServer)
            SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<NightVisionComponent, NightVisionToggleEvent>(OnActionToggle);

    }

    [ValidatePrototypeId<EntityPrototype>]
    private const string SwitchNightVisionAction = "SwitchNightVision";

    private void OnComponentStartup(EntityUid uid, NightVisionComponent component, ComponentStartup args)
    {
        if (component.IsToggle)
            _actionsSystem.AddAction(uid, ref component.ActionContainer, SwitchNightVisionAction);
    }

    private void OnActionToggle(EntityUid uid, NightVisionComponent component, NightVisionToggleEvent args)
    {
        component.IsNightVision = !component.IsNightVision;
        var changeEv = new NightVisionToggledEvent(component.IsNightVision);
        RaiseLocalEvent(uid, ref changeEv);
        Dirty(uid, component);
    }

    [PublicAPI]
    public void UpdateIsNightVision(EntityUid uid, NightVisionComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var old = component.IsNightVision;


        var ev = new CanVisionAttemptEvent();
        RaiseLocalEvent(uid, ev);
        component.IsNightVision = ev.CanEnableNightVision;

        if (old == component.IsNightVision)
            return;

        var changeEv = new NightVisionToggledEvent(component.IsNightVision);
        RaiseLocalEvent(uid, ref changeEv);
        Dirty(uid, component);
    }
}

[ByRefEvent]
public record struct NightVisionToggledEvent(bool Enabled);

[PublicAPI, ByRefEvent]
public sealed class NightVisionDeviceUpdateVisualsEvent : EntityEventArgs
{

}

public sealed class CanVisionAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public bool CanEnableNightVision => Cancelled;
    public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
}
