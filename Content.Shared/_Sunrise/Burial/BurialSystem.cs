using Content.Shared.ActionBlocker;
using Content.Shared.Burial.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;

namespace Content.Shared.Burial;

public sealed class BurialSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedEntityStorageSystem _storageSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GraveComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GraveComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GraveComponent, AfterInteractUsingEvent>(OnAfterInteractUsing, before: new[] { typeof(PlaceableSurfaceSystem) });
        SubscribeLocalEvent<GraveComponent, GraveDiggingDoAfterEvent>(OnGraveDigging);

        SubscribeLocalEvent<GraveComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<GraveComponent, StorageCloseAttemptEvent>(OnCloseAttempt);
        SubscribeLocalEvent<GraveComponent, StorageAfterOpenEvent>(OnAfterOpen);
        SubscribeLocalEvent<GraveComponent, StorageAfterCloseEvent>(OnAfterClose);

        SubscribeLocalEvent<GraveComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
    }

    private void OnInteractUsing(EntityUid uid, GraveComponent component, InteractUsingEvent args)
    {
        //* Temporary
        // if (args.Handled || _doAfterSystem.IsRunning(component.ShovelDiggingDoAfterId))
        //     return;

        //* Coderabbit start
        if (args.Handled)
            return;

        if (IsAnyDiggingActive(component))
        {
            args.Handled = true;
            return;
        }
        //* Coderabbit end

        if (TryComp<ShovelComponent>(args.Used, out var shovel))
        {
            var doAfterEventArgs = new DoAfterArgs(
                EntityManager,
                args.User,
                component.DigDelay / shovel.SpeedModifier,
                new GraveDiggingDoAfterEvent(),
                uid,
                target: uid,
                used: args.Used
            )
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };

            EnsureAndStartDiggingSound(uid, args.User, component);

            if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var startedDoAfterId))
            {
                StopDiggingSound(component);
                return;
            }

            component.ShovelDiggingDoAfterId = startedDoAfterId;
            StartDigging(uid, args.User, args.Used, component);
            Dirty(uid, component);
        }
        else
        {
            _popupSystem.PopupClient(Loc.GetString("grave-digging-requires-tool", ("grave", args.Target)), uid, args.User);
        }

        args.Handled = true;
    }

    private void OnRelayMovement(EntityUid uid, GraveComponent component, ref ContainerRelayMovementEntityEvent args)
    {
        //* Coderabbit suggest start
        // if (_doAfterSystem.IsRunning(component.HandDiggingDoAfterId))

        if (IsAnyDiggingActive(component))
            return;
        //* Coderabbit suggest end

        if (!_actionBlocker.CanMove(args.Entity))
            return;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            args.Entity,
            component.DigDelay / component.DigOutByHandModifier,
            new GraveDiggingDoAfterEvent(),
            uid,
            target: uid,
            used: null
        )
        {
            NeedHand = false,
            BreakOnMove = true,
            BreakOnHandChange = false,
            BreakOnDamage = false
        };

        EnsureAndStartDiggingSound(uid, args.Entity, component);

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var startedDoAfterId))
        {
            StopDiggingSound(component);
            return;
        }

        component.HandDiggingDoAfterId = startedDoAfterId;
        StartDigging(uid, args.Entity, null, component);
    }

    private void OnGraveDigging(EntityUid uid, GraveComponent component, GraveDiggingDoAfterEvent args)
    {
        if (args.Used != null && component.ShovelDiggingDoAfterId != null)
        {
            component.ShovelDiggingDoAfterId = null;
        }
        else if (args.Used == null && component.HandDiggingDoAfterId != null)
        {
            component.HandDiggingDoAfterId = null;
        }

        if (!IsAnyDiggingActive(component))
        {
            StopDiggingSound(component);
        }

        if (args.Cancelled || args.Handled)
            return;

        component.DiggingComplete = true;
        Dirty(uid, component);

        if (args.Used != null)
            _storageSystem.ToggleOpen(args.User, uid);
        else
            _storageSystem.TryOpenStorage(args.User, uid);
    }

    private void StartDigging(EntityUid uid, EntityUid user, EntityUid? used, GraveComponent component)
    {
        if (used != null)
        {
            var selfMessage = Loc.GetString("grave-start-digging-user", ("grave", uid), ("tool", used));
            var othersMessage = Loc.GetString("grave-start-digging-others", ("user", user), ("grave", uid), ("tool", used));
            _popupSystem.PopupPredicted(selfMessage, othersMessage, user, user);
        }
        else
        {
            _popupSystem.PopupClient(Loc.GetString("grave-start-digging-user-trapped", ("grave", uid)), user, user, PopupType.Medium);
        }
    }

    private void EnsureAndStartDiggingSound(EntityUid graveUid, EntityUid userUid, GraveComponent graveComp)
    {
        if (graveComp.Stream == null)
        {
            graveComp.Stream = _audioSystem.PlayPredicted(graveComp.DigSound, graveUid, userUid)?.Entity;
        }
    }

    private void StopDiggingSound(GraveComponent graveComp)
    {
        graveComp.Stream = _audioSystem.Stop(graveComp.Stream);
    }

    private bool IsAnyDiggingActive(GraveComponent graveComp)
    {
        return _doAfterSystem.IsRunning(graveComp.ShovelDiggingDoAfterId) ||
               _doAfterSystem.IsRunning(graveComp.HandDiggingDoAfterId);
    }

    private void OnAfterInteractUsing(EntityUid uid, GraveComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ShovelComponent>(args.Used))
            args.Handled = true;
    }

    private void OnActivate(EntityUid uid, GraveComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        _popupSystem.PopupClient(Loc.GetString("grave-digging-requires-tool", ("grave", args.Target)), uid, args.User);
        args.Handled = true;
    }

    private void OnOpenAttempt(EntityUid uid, GraveComponent component, ref StorageOpenAttemptEvent args)
    {
        if (component.DiggingComplete)
            return;

        args.Cancelled = true;
    }

    private void OnCloseAttempt(EntityUid uid, GraveComponent component, ref StorageCloseAttemptEvent args)
    {
        if (component.DiggingComplete)
            return;

        args.Cancelled = true;
    }

    private void OnAfterOpen(EntityUid uid, GraveComponent component, ref StorageAfterOpenEvent args)
    {
        component.DiggingComplete = false;
        Dirty(uid, component);
    }

    private void OnAfterClose(EntityUid uid, GraveComponent component, ref StorageAfterCloseEvent args)
    {
        component.DiggingComplete = false;
        Dirty(uid, component);
    }
}
