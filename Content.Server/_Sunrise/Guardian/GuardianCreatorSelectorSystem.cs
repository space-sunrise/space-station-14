using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared._Sunrise.Guardian;
using Content.Shared.DoAfter;
using Content.Shared.Guardian;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server._Sunrise.Guardian;

public sealed class GuardianCreatorSelectorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianCreatorSelectorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GuardianCreatorSelectorComponent, UseInHandEvent>(OnUseInHand, before: [typeof(GuardianSystem)]);
        SubscribeLocalEvent<GuardianCreatorSelectorComponent, AfterInteractEvent>(OnAfterInteract, before: [typeof(GuardianSystem)]);

        Subs.BuiEvents<GuardianCreatorSelectorComponent>(GuardianCreatorSelectorUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
            subs.Event<GuardianCreatorSelectorConfirmMessage>(OnConfirm);
        });
    }

    private void OnMapInit(Entity<GuardianCreatorSelectorComponent> ent, ref MapInitEvent args)
    {
        EnsureSelected(ent.Comp);
    }

    private void OnUseInHand(Entity<GuardianCreatorSelectorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryOpenSelector(ent.AsNullable(), args.User, args.User);
    }

    private void OnAfterInteract(Entity<GuardianCreatorSelectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is null || !args.CanReach)
            return;

        args.Handled = true;
        TryOpenSelector(ent.AsNullable(), args.User, args.Target.Value);
    }

    private void OnUiOpened(Entity<GuardianCreatorSelectorComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnUiClosed(Entity<GuardianCreatorSelectorComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.PendingTargets.Remove(args.Actor);
    }

    private void OnConfirm(Entity<GuardianCreatorSelectorComponent> ent, ref GuardianCreatorSelectorConfirmMessage args)
    {
        if (args.Actor is not { Valid: true } user)
            return;

        var target = ent.Comp.PendingTargets.GetValueOrDefault(user, user);
        if (!TryGetOption(ent.Comp, args.Prototype, out var option))
            return;

        if (!CanStartInjection(ent, user, target))
            return;

        ent.Comp.SelectedPrototype = option.Prototype;

        var creator = Comp<GuardianCreatorComponent>(ent.Owner);
        creator.GuardianProto = option.Prototype;

        _ui.CloseUi(ent.Owner, GuardianCreatorSelectorUiKey.Key, user);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            creator.InjectionDelay,
            new GuardianCreatorDoAfterEvent(),
            ent.Owner,
            target: target,
            used: ent.Owner)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
        });
    }

    public bool TryOpenSelector(Entity<GuardianCreatorSelectorComponent?> ent, EntityUid user, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanOpenSelector(ent.Owner, ent.Comp, user, target))
            return false;

        ent.Comp.PendingTargets[user] = target;
        EnsureSelected(ent.Comp);

        if (!_ui.TryOpenUi(ent.Owner, GuardianCreatorSelectorUiKey.Key, user))
            return false;

        UpdateUserInterface(ent);
        return true;
    }

    public bool CanOpenSelector(
        EntityUid uid,
        GuardianCreatorSelectorComponent component,
        EntityUid user,
        EntityUid target,
        bool quiet = false)
    {
        if (!TryComp<GuardianCreatorComponent>(uid, out var creator))
            return false;

        if (component.Options.Count == 0)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("guardian-selector-no-options"), uid, user, PopupType.MediumCaution);

            return false;
        }

        if (creator.Used)
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("guardian-activator-empty-invalid-creation"), user, user);

            return false;
        }

        if (!HasComp<CanHostGuardianComponent>(target))
        {
            if (!quiet)
            {
                var msg = Loc.GetString("guardian-activator-invalid-target",
                    ("entity", Identity.Entity(target, EntityManager, user)));

                _popup.PopupEntity(msg, user, user);
            }

            return false;
        }

        if (HasComp<GuardianHostComponent>(target))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("guardian-already-present-invalid-creation"), user, user);

            return false;
        }

        return true;
    }

    private bool CanStartInjection(Entity<GuardianCreatorSelectorComponent> ent, EntityUid user, EntityUid target)
    {
        if (!CanOpenSelector(ent.Owner, ent.Comp, user, target))
            return false;

        if (!_hands.IsHolding(user, ent.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString("guardian-selector-not-held"), ent.Owner, user, PopupType.MediumCaution);
            return false;
        }

        if (!_interaction.InRangeUnobstructed(user, target))
        {
            var msg = Loc.GetString("guardian-activator-invalid-target",
                ("entity", Identity.Entity(target, EntityManager, user)));

            _popup.PopupEntity(msg, user, user);
            return false;
        }

        return true;
    }

    private void UpdateUserInterface(Entity<GuardianCreatorSelectorComponent> ent)
    {
        EnsureSelected(ent.Comp);

        var options = new List<GuardianCreatorSelectorEntryState>(ent.Comp.Options.Count);
        var selectedIndex = 0;

        for (var i = 0; i < ent.Comp.Options.Count; i++)
        {
            var option = ent.Comp.Options[i];
            if (option.Prototype == ent.Comp.SelectedPrototype)
                selectedIndex = i;

            options.Add(new GuardianCreatorSelectorEntryState(
                option.Prototype,
                option.Name,
                option.Description ?? string.Empty,
                option.Details));
        }

        _ui.SetUiState(ent.Owner, GuardianCreatorSelectorUiKey.Key,
            new GuardianCreatorSelectorBuiState(options, selectedIndex));
    }

    private static void EnsureSelected(GuardianCreatorSelectorComponent component)
    {
        if (component.Options.Count == 0)
        {
            component.SelectedPrototype = null;
            return;
        }

        foreach (var option in component.Options)
        {
            if (component.SelectedPrototype == option.Prototype)
                return;
        }

        component.SelectedPrototype = component.Options[0].Prototype;
    }

    private static bool TryGetOption(
        GuardianCreatorSelectorComponent component,
        string prototype,
        out GuardianCreatorSelectorOption option)
    {
        foreach (var candidate in component.Options)
        {
            if (candidate.Prototype == prototype)
            {
                option = candidate;
                return true;
            }
        }

        option = default!;
        return false;
    }
}
