using Content.Shared.AutoInjector.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.AutoInjector.Systems;

public class SharedAutoInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoInjectorSlotComponent, ComponentInit>(OnSlotInit);
        SubscribeLocalEvent<AutoInjectorSlotComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        
        // Listen for damage changes to trigger auto-injections
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnSlotInit(EntityUid uid, AutoInjectorSlotComponent component, ComponentInit args)
    {
        // Initialize storage container for auto-injectors
        var container = _containers.EnsureContainer<Container>(uid, "autoinjector_slots");
    }

    private void OnGetVerbs(EntityUid uid, AutoInjectorSlotComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;

        // Add verb to load auto-injectors
        if (_hands.TryGetActiveItem(user, out var held) &&
            HasComp<AutoInjectorTriggerComponent>(held.Value) &&
            HasComp<InjectorComponent>(held.Value))
        {
            if (component.StoredInjectors.Count < component.MaxSlots)
            {
                InteractionVerb loadVerb = new()
                {
                    Text = Loc.GetString("auto-injector-verb-load"),
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                    Act = () => LoadAutoInjector(uid, component, user, held.Value),
                    Priority = 2
                };
                args.Verbs.Add(loadVerb);
            }
        }

        // Add verb to unload auto-injectors
        if (component.StoredInjectors.Count > 0)
        {
            InteractionVerb unloadVerb = new()
            {
                Text = Loc.GetString("auto-injector-verb-unload"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () => UnloadAutoInjector(uid, component, user),
                Priority = 1
            };
            args.Verbs.Add(unloadVerb);
        }
    }

    private void LoadAutoInjector(EntityUid slotEnt, AutoInjectorSlotComponent component, EntityUid user, EntityUid injector)
    {
        if (component.StoredInjectors.Count >= component.MaxSlots)
        {
            _popup.PopupClient(Loc.GetString("auto-injector-slots-full"), slotEnt, user);
            return;
        }

        if (!_hands.TryDrop(user, injector))
            return;

        var container = _containers.GetContainer(slotEnt, "autoinjector_slots");
        if (_containers.Insert(injector, container))
        {
            component.StoredInjectors.Add(injector);
            _popup.PopupClient(Loc.GetString("auto-injector-loaded"), slotEnt, user);
            Dirty(slotEnt, component);
        }
    }

    private void UnloadAutoInjector(EntityUid slotEnt, AutoInjectorSlotComponent component, EntityUid user)
    {
        if (component.StoredInjectors.Count == 0)
            return;

        var injector = component.StoredInjectors[0];
        component.StoredInjectors.RemoveAt(0);

        var container = _containers.GetContainer(slotEnt, "autoinjector_slots");
        if (_containers.Remove(injector, container))
        {
            _hands.TryPickup(user, injector);
            _popup.PopupClient(Loc.GetString("auto-injector-unloaded"), slotEnt, user);
            Dirty(slotEnt, component);
        }
    }

    private void OnDamageChanged(EntityUid uid, DamageableComponent damageable, DamageChangedEvent args)
    {
        // Only process on server side
        if (_net.IsClient)
            return;

        // Check if entity is wearing clothing with auto-injector slots
        if (!_inventory.TryGetSlotEntity(uid, "outerClothing", out var clothing) ||
            !TryComp<AutoInjectorSlotComponent>(clothing, out var slotComp))
            return;

        // Check cooldown
        var currentTime = _timing.CurTime;
        if ((currentTime - slotComp.LastInjectionTime).TotalSeconds < slotComp.InjectionCooldown)
            return;

        // Find the best auto-injector to use
        var bestInjector = FindBestAutoInjector(slotComp, damageable);
        if (bestInjector == null)
            return;

        // Raise an event for the server system to handle the actual injection
        var ev = new AutoInjectionTriggeredEvent
        {
            Target = uid,
            Clothing = clothing.Value,
            Injector = bestInjector.Value
        };
        RaiseLocalEvent(clothing.Value, ref ev);
    }

    private EntityUid? FindBestAutoInjector(AutoInjectorSlotComponent slotComp, DamageableComponent damageable)
    {
        EntityUid? bestInjector = null;
        int highestPriority = -1;

        foreach (var injectorEnt in slotComp.StoredInjectors)
        {
            if (!TryComp<AutoInjectorTriggerComponent>(injectorEnt, out var trigger) || trigger.IsUsed)
                continue;

            // Check if trigger conditions are met
            bool shouldTrigger = false;

            // Check total damage threshold
            var totalDamage = damageable.Damage.GetTotal();
            if (totalDamage.Float() >= trigger.TotalDamageThreshold)
                shouldTrigger = true;

            // Check specific damage type thresholds
            foreach (var (damageType, threshold) in trigger.DamageTypeThresholds)
            {
                var damageValue = damageable.Damage.DamageDict.GetValueOrDefault(damageType, FixedPoint2.Zero);
                if (damageValue.Float() >= threshold)
                {
                    shouldTrigger = true;
                    break;
                }
            }

            if (shouldTrigger && trigger.Priority > highestPriority)
            {
                bestInjector = injectorEnt;
                highestPriority = trigger.Priority;
            }
        }

        return bestInjector;
    }
}

/// <summary>
/// Event raised when an auto-injector should be triggered.
/// Handled by the server-side system.
/// </summary>
[ByRefEvent]
public record struct AutoInjectionTriggeredEvent
{
    public EntityUid Target;
    public EntityUid Clothing;
    public EntityUid Injector;
}