using Content.Shared._Sunrise.SiliconStanding;
using Robust.Server.GameObjects;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.DoAfter;
using Content.Shared.ActionBlocker;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Content.Server.DoAfter;
using Robust.Shared.Log;
using System.Numerics;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ToggleStandingEvent>(OnToggle);
        SubscribeLocalEvent<SiliconRestingDoAfterComponent, SiliconRestingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestStart);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestEnd);
        SubscribeLocalEvent<SiliconRestingComponent, UpdateCanMoveEvent>(OnCanMove);
    }

    private void OnToggle(ToggleStandingEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        if (!HasComp<BorgChassisComponent>(uid))
            return;

        var isStandingUp = HasComp<SiliconRestingComponent>(uid);
        var delay = isStandingUp ? 0.5f : 1.0f;

        EnsureComp<SiliconRestingDoAfterComponent>(uid);

        var doAfter = new DoAfterArgs(
            EntityManager,
            uid,
            delay,
            new SiliconRestingDoAfterEvent(),
            uid,
            null
        )
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            Broadcast = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            RemComp<SiliconRestingDoAfterComponent>(uid);
        }
    }

    private void Toggle(EntityUid uid)
    {
        Log.Info($"[TOGGLE SENT] uid={uid}");
        if (HasComp<SiliconRestingComponent>(uid))
        {
            RemComp<SiliconRestingComponent>(uid);
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, false);
            _actionBlocker.UpdateCanMove(uid);
        }
        else
        {
            EnsureComp<SiliconRestingComponent>(uid);
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, true);
        }
    }
    private void OnCanMove(EntityUid uid, SiliconRestingComponent component, ref UpdateCanMoveEvent args)
    {
        if (HasComp<SiliconRestingComponent>(uid))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, SiliconRestingDoAfterComponent comp, SiliconRestingDoAfterEvent ev)
    {
        RemComp<SiliconRestingDoAfterComponent>(uid);

        ev.Success = !ev.Cancelled;

        if (ev.Cancelled)
        {
            Log.Info($"[DOAFTER RESULT] uid={uid} cancelled={ev.Cancelled}");
            return;
        }

        Toggle(uid);
    }
    private void OnRestEnd(EntityUid uid, SiliconRestingComponent component, ComponentShutdown args)
    {
        _actionBlocker.UpdateCanMove(uid);
    }

    private void OnRestStart(EntityUid uid, SiliconRestingComponent component, ComponentStartup args)
    {
        _actionBlocker.UpdateCanMove(uid);
    }
}