using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Client.GameObjects;
using Content.Shared._Sunrise.SiliconStanding;

namespace Content.Shared._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding,
                InputCmdHandler.FromDelegate(HandleToggle, handle: false))
            .Register<SiliconStandingSystem>();

        SubscribeLocalEvent<SiliconStandingComponent, UpdateCanMoveEvent>(OnMove);
        SubscribeLocalEvent<SiliconStandingComponent, SiliconRestStartEvent>(OnRestStart);
        SubscribeLocalEvent<SiliconStandingComponent, SiliconRestEndEvent>(OnRestEnd);
    }

    private void OnRestStart(Entity<SiliconStandingComponent> ent, ref SiliconRestStartEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.LayerSetState((ent, sprite), "robot_rest");
    }

    private void OnRestEnd(Entity<SiliconStandingComponent> ent, ref SiliconRestEndEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.LayerSetState((ent, sprite), "robot");
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
}