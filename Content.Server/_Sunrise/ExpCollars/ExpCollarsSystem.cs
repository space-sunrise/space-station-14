using System.Linq;
using System.Threading.Tasks;
using Content.Server.Body.Systems;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Wires;
using Content.Shared.Clothing;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server._Sunrise.ExpCollars;

/// <summary>
/// Система для взрывного ошейника.
/// </summary>
public sealed class ExpCollarsSystem : EntitySystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ExpCollarComponent, AfterInteractEvent>(OnInteraction);
        SubscribeLocalEvent<ExpCollarComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ExpCollarComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ExpCollarComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ExpCollarUserComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private async void OnMobStateChanged(EntityUid uid, ExpCollarUserComponent component, MobStateChangedEvent args)
    {
        if (component.Tool == null || !(args.OldMobState == MobState.Alive && args.NewMobState != MobState.Alive))
            return;

        if (!TryComp<ExpCollarComponent>(component.Tool, out var expCollarComponent))
            return;

        if (expCollarComponent.Armed == false || !expCollarComponent.IsHost)
            return;

        Destroy(component.Tool.Value);
        foreach (var linkedCollar in expCollarComponent.Linked)
        {
            _popup.PopupEntity(Loc.GetString("expcollar-kill"), linkedCollar, PopupType.LargeCaution);
            Destroy(linkedCollar);
        }
    }

    private async void Destroy(EntityUid uid)
    {
        if (!TryComp<ExpCollarComponent>(uid, out var collar))
            return;
        if (collar.Wearer == null)
        {
            // Больше нельзя использовать
            RemComp<ExpCollarComponent>(uid);
            return;
        }

        if (collar.Armed == false)
            return;

        _popup.PopupEntity(Loc.GetString("expcollar-boom"), uid, PopupType.LargeCaution);
        await Task.Delay(TimeSpan.FromSeconds(1));

        for (var i = 10; i > 0; i--)
        {
            _popup.PopupEntity(Loc.GetString("expcollar-popup", ("timer", i)), uid, PopupType.LargeCaution);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (HasComp<ExpCollarUserComponent>(collar.Wearer.Value))
        {
            _bodySystem.GibBody(collar.Wearer.Value);
            _trigger.Trigger(uid);
        }
    }

    private void OnInteraction(EntityUid uid, ExpCollarComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !component.IsHost || args.Target == null || !TryComp(args.Target, out ExpCollarComponent? targetComp) || targetComp.IsHost)
            return;

        if (targetComp.Linked.Count != 0)
        {
            _popup.PopupEntity(Loc.GetString("expcollar-connected"), args.Target.Value, PopupType.SmallCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("expcollar-connect"), args.Target.Value, PopupType.MediumCaution);
        component.Linked.Add(args.Target.Value);
        targetComp.Linked.Add(uid);
    }

    private void OnEquipped(EntityUid uid, ExpCollarComponent component, ClothingGotEquippedEvent args)
    {
        if (TryComp<ExpCollarUserComponent>(args.Wearer, out _))  // Уже носит... как..
            return;

        var comp = EnsureComp<ExpCollarUserComponent>(args.Wearer);
        comp.Tool = uid;
        component.Wearer = args.Wearer;

        if (!_mobState.IsAlive(args.Wearer))
            return;
        _popup.PopupEntity(Loc.GetString("expcollar-bolts-up"), args.Wearer, PopupType.LargeCaution);
        component.Bolts = true;
        component.Virgin = false;
        EnsureComp<UnremoveableComponent>(uid);

        if (component.Armed == false && component.IsHost == false)
        {
            var a = component.Linked.FirstOrDefault();
            if (!TryComp<ExpCollarComponent>(a, out var hostCollarComponent))
                return;
            if (hostCollarComponent.Armed == true)
            {
                component.Armed = true;
                _popup.PopupEntity(Loc.GetString("expcollar-armed"), uid, PopupType.LargeCaution);
            }
        }

        if (component.IsHost)
        {
            component.Armed = true;
            _popup.PopupEntity(Loc.GetString("expcollar-armed"), args.Wearer, PopupType.LargeCaution);
            foreach (var i in component.Linked)
            {
                if (!TryComp<ExpCollarComponent>(i, out var expCollarComponent))
                    continue;

                if (expCollarComponent.Wearer == null)
                    continue;

                expCollarComponent.Armed = true;
                EnsureComp<UnremoveableComponent>(i);
                _popup.PopupEntity(Loc.GetString("expcollar-armed"), i, PopupType.LargeCaution);
            }
        }
    }

    private void OnUnequipped(EntityUid uid, ExpCollarComponent component, ClothingGotUnequippedEvent args)
    {
        if (!TryComp<ExpCollarUserComponent>(args.Wearer, out var expCollarUserComponent)) // На него насильно надели...
            return;

        component.Wearer = null;
        RemComp<ExpCollarUserComponent>(args.Wearer);
    }

    private void OnShutdown(EntityUid uid, ExpCollarComponent component, ComponentShutdown args)
    {
        if (component.Wearer == null)
            return;

        if (!TryComp<ExpCollarUserComponent>(component.Wearer, out var expCollarUserComponent)) // На него насильно надели...
            return;
        RemComp<ExpCollarUserComponent>(component.Wearer.Value);
    }

    public bool Electrocute(EntityUid target, Wire wire, ExpCollarComponent comp)
    {
        if (comp.Wearer != null)
            _electrocution.TryDoElectrocution(comp.Wearer.Value, null, 20, TimeSpan.FromSeconds(2), false, ignoreInsulation: true);
        return false;
    }

    public bool Arm(EntityUid target, Wire wire, ExpCollarComponent component)
    {
        if (component.Armed)
        {
            component.Armed = false;
            _popup.PopupEntity(Loc.GetString("expcollar-disarmed"), target, PopupType.LargeCaution);
        }
        else if (component is { Armed: false, Virgin: true })
        {
            component.Armed = true;
        }
        else
        {
            return false;
        }

        component.Virgin = false;

        return true;
    }

    public bool Bolt(EntityUid target, Wire wire, ExpCollarComponent component)
    {
        if (component.Bolts)
        {
            component.Bolts = false;
            if (HasComp<UnremoveableComponent>(target))
                RemComp<UnremoveableComponent>(target);

            if (component is { ActiveCooldown: true, Wearer: not null })
            {
                if (!TryComp<ExpCollarUserComponent>(component.Wearer, out var expCollarUserComponent))
                    RemComp<ExpCollarUserComponent>(component.Wearer.Value);
            }
        }

        return true;
    }
}
