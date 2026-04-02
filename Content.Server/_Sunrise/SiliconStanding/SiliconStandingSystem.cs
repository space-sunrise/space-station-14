using Content.Shared._Sunrise.SiliconStanding;
using Robust.Server.GameObjects;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Physics.Components;
using Content.Server.DoAfter;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ToggleStandingEvent>(OnToggle);
        SubscribeLocalEvent<SiliconRestingComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<SiliconRestingDoAfterComponent, SiliconRestingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SiliconRestingDoAfterComponent, UpdateCanMoveEvent>(OnDoAfterMoveBlock);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestStart);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestEnd);
    }

    private void OnToggle(ToggleStandingEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        if (!HasComp<BorgChassisComponent>(uid))
            return;

        if (HasComp<SiliconRestingDoAfterComponent>(uid))
        {
            RemComp<SiliconRestingDoAfterComponent>(uid);
        }

        var comp = EnsureComp<SiliconRestingDoAfterComponent>(uid);

        var isStandingUp = HasComp<SiliconRestingComponent>(uid);

        var delay = isStandingUp ? 0.5f : 1.0f;

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
        if (HasComp<SiliconRestingComponent>(uid))
        {
            RemComp<SiliconRestingComponent>(uid);
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, false);
        }
        else
        {
            EnsureComp<SiliconRestingComponent>(uid);
            _appearance.SetData(uid, SiliconStandingVisuals.Resting, true);
        }
    }
    private void OnCanMove(EntityUid uid, SiliconRestingComponent component, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, SiliconRestingDoAfterComponent comp, SiliconRestingDoAfterEvent ev)
    {
        RemComp<SiliconRestingDoAfterComponent>(uid);

        if (ev.Cancelled)
            return;

        Toggle(uid);
    }
    private void OnDoAfterMoveBlock(EntityUid uid, SiliconRestingDoAfterComponent comp, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnRestStart(EntityUid uid, SiliconRestingComponent comp, ComponentStartup args)
    {
        if (TryComp<InputMoverComponent>(uid, out var mover))
            mover.CanMove = false;
    }

    private void OnRestEnd(EntityUid uid, SiliconRestingComponent comp, ComponentShutdown args)
    {
        if (TryComp<InputMoverComponent>(uid, out var mover))
            mover.CanMove = true;
    }
}