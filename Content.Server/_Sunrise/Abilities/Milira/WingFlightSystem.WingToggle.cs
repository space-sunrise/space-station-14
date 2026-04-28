using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Toggleable;
using Content.Shared._Sunrise.Abilities.Milira;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Abilities.Milira;

/// <summary>
/// Система для переключения крыльев расы милира.
/// </summary>
public sealed partial class WingToggleSystem : SharedWingFlightSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingToggleComponent, MapInitEvent>(OnWingToggleMapInit);
        SubscribeLocalEvent<WingToggleComponent, ComponentShutdown>(OnWingToggleShutdown);
        SubscribeLocalEvent<WingToggleComponent, ToggleActionEvent>(OnWingToggleAction);
        SubscribeLocalEvent<WingToggleComponent, WingForceClose>(OnWingClose);
    }

    private void OnWingToggleMapInit(Entity<WingToggleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
        UpdateWingToggleAction(ent);
    }

    private void OnWingToggleShutdown(Entity<WingToggleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnWingToggleAction(Entity<WingToggleComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ActionEntity == null || args.Action.Owner != ent.Comp.ActionEntity.Value)
            return;

        args.Handled = TryToggleWings(ent);
    }

    public bool TryToggleWings(Entity<WingToggleComponent> ent, HumanoidAppearanceComponent? humanoid = null, bool forceClose = false)
    {
        if (!Resolve(ent.Owner, ref humanoid, false))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) || markings.Count == 0)
            return false;

        if (TryComp<WingFlightComponent>(ent, out var wingFlight) && wingFlight.InertiaActive && !forceClose)
            return false;

        if ((!forceClose || !ent.Comp.WingsOpened) && !CanOpenWings(ent))
        {
            _popup.PopupEntity(Loc.GetString("wing-toggle-open-blocked"), ent, ent, PopupType.Medium);
            return false;
        }

        var openTarget = !ent.Comp.WingsOpened;
        var suffix = ent.Comp.Suffix;
        var changed = false;

        for (var i = 0; i < markings.Count; i++)
        {
            var current = markings[i].MarkingId;
            var desired = openTarget
                ? (current.EndsWith(suffix) ? current : $"{current}{suffix}")
                : (current.EndsWith(suffix) ? current[..^suffix.Length] : current);

            if (!_prototype.HasIndex<MarkingPrototype>(desired))
                continue;

            if (desired == current)
                continue;

            _appearance.SetMarkingId(ent.Owner, MarkingCategories.Tail, i, desired, humanoid: humanoid);
            changed = true;
        }

        if (!changed)
            return false;

        ent.Comp.WingsOpened = openTarget;
        Dirty(ent);
        UpdateWingToggleAction(ent);

        if (ent.Comp.WingsOpened)
        {
            EnsureComp<WingFlightComponent>(ent.Owner);
            EnsureComp<JumpAbilityComponent>(ent.Owner);
        }
        else
        {
            RemCompDeferred<WingFlightComponent>(ent.Owner);
            RemCompDeferred<JumpAbilityComponent>(ent.Owner);
        }
        return true;
    }

    private bool CanOpenWings(Entity<WingToggleComponent> ent)
    {
        if (ent.Comp.BlockedSlots == null || ent.Comp.BlockedSlots.Count == 0)
            return true;

        foreach (var slot in ent.Comp.BlockedSlots)
        {
            if (!_inventory.TryGetSlotEntity(ent.Owner, slot, out var equippedEntity))
                continue;

            if (ent.Comp.AllowedTag == null || !_tagSystem.HasTag(equippedEntity.Value, ent.Comp.AllowedTag.Value))
                return false;
        }

        return true;
    }

    private void UpdateWingToggleAction(Entity<WingToggleComponent> ent)
    {
        if (ent.Comp.ActionEntity == null)
            return;

        _actions.SetToggled(ent.Comp.ActionEntity.Value, ent.Comp.WingsOpened);
    }

    private void OnWingClose(Entity<WingToggleComponent> ent, ref WingForceClose args)
    {
        TryToggleWings(ent, forceClose: true);
    }
}
