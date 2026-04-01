using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.GameObjects;
using Content.Shared._Sunrise.SiliconStanding;

namespace Content.Shared._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding,
                InputCmdHandler.FromDelegate(HandleToggle, handle: false))
            .Register<SiliconStandingSystem>();
    }

    private void HandleToggle(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } uid)
            return;

        Toggle(uid);
    }

    public void Toggle(EntityUid uid)
        {
            if (!TryComp<SiliconStandingComponent>(uid, out var comp))
                return;

            comp.Active = !comp.Active;
            Dirty(uid, comp);

            _appearance.SetData(uid, SiliconStandingVisuals.Resting, comp.Active);
        }

    private void OnMove(Entity<SiliconStandingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (!ent.Comp.Active)
            return;

        args.Cancel();
    }
}