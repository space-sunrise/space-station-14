using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Toggleable;
using Content.Shared._Sunrise.Abilities.Milira;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Abilities.Milira;

/// <summary>
/// Система, позволяющая раскрывать и складывать крылья путём замены маркингов.
/// </summary>
public sealed class WingToggleSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WingToggleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WingToggleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WingToggleComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnMapInit(EntityUid uid, WingToggleComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    private void OnToggleAction(EntityUid uid, WingToggleComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryToggleWings(uid, component);
    }

    public bool TryToggleWings(EntityUid uid, WingToggleComponent? component = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref component, ref humanoid, false))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings) || markings.Count == 0)
            return false;

        if (!component.WingsOpened)
        {
            if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var outer) && outer != null)
            {
                var sharedSystem = EntitySystem.Get<SharedWingToggleSystem>();
                sharedSystem.ShowOpenBlockedPopup(uid);
                return false;
            }
        }

        var openTarget = !component.WingsOpened;
        var suffix = component.Suffix;
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

            _appearance.SetMarkingId(uid, MarkingCategories.Tail, i, desired, humanoid: humanoid);
            changed = true;
        }

        if (!changed && openTarget != component.WingsOpened)
            return false;

        component.WingsOpened = openTarget;
        return true;
    }

    private void OnShutdown(EntityUid uid, WingToggleComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }
}
