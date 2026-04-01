using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Content.Shared._Sunrise.SiliconStanding;

namespace Content.Shared._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding,
                InputCmdHandler.FromDelegate(HandleToggle, handle: false))
            .Register<SiliconStandingSystem>();

        SubscribeLocalEvent<SiliconStandingComponent, UpdateCanMoveEvent>(OnMove);
        SubscribeLocalEvent<SiliconStandingComponent, JumpAttemptEvent>(OnJump);
    }

    private void HandleToggle(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } uid)
            return;

        Toggle(uid);
    }

    public void Toggle(EntityUid uid)
    {
        if (!HasComp<SiliconStandingComponent>(uid))
            return;

        var comp = Comp<SiliconStandingComponent>(uid);

        comp.Active = !comp.Active;
        Dirty(uid, comp);

        if (comp.Active)
            RaiseLocalEvent(uid, new SiliconRestStartEvent());
        else
            RaiseLocalEvent(uid, new SiliconRestEndEvent());
    }

    private void OnMove(Entity<SiliconStandingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.Cancel();
    }

    private void OnJump(Entity<SiliconStandingComponent> ent, ref JumpAttemptEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.Cancel();
    }
}